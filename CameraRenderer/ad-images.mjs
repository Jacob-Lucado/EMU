import { lookup } from 'node:dns';
import http from 'node:http';
import https from 'node:https';
import { BlockList, isIP } from 'node:net';
import { createAssetCache } from './asset-cache.mjs';

// Room ads show an image from a web address a staff member typed. The service downloads it for the page, so it
// must never reach the hotel's own network: only public addresses, default ports and real images.
const MAX_ADDRESS = 500;
const MAX_IMAGE = 8 * 1024 * 1024;
const DOWNLOAD_MS = 8000;
const MAX_REDIRECTS = 3;
const KEEP_MS = 10 * 60 * 1000;

// Separate lists: one BlockList would also match IPv4 addresses against the IPv4-mapped IPv6 range.
const blocked = { 4: new BlockList(), 6: new BlockList() };
for (const [network, prefix] of [['0.0.0.0', 8], ['10.0.0.0', 8], ['100.64.0.0', 10], ['127.0.0.0', 8], ['169.254.0.0', 16], ['172.16.0.0', 12],
    ['192.0.0.0', 24], ['192.0.2.0', 24], ['192.168.0.0', 16], ['198.18.0.0', 15], ['198.51.100.0', 24], ['203.0.113.0', 24], ['224.0.0.0', 4], ['240.0.0.0', 4]]) blocked[4].addSubnet(network, prefix, 'ipv4');
for (const [network, prefix] of [['::', 127], ['::ffff:0:0', 96], ['64:ff9b::', 96], ['64:ff9b:1::', 48], ['100::', 64], ['2001::', 32], ['2001:db8::', 32],
    ['2002::', 16], ['fc00::', 7], ['fe80::', 10], ['fec0::', 10], ['ff00::', 8]]) blocked[6].addSubnet(network, prefix, 'ipv6');

export function publicAddress(address) {
    const family = isIP(address);
    return family !== 0 && !blocked[family].check(address, family === 4 ? 'ipv4' : 'ipv6');
}

// The client loads every room ad image over https, so the page asks for that address.
export function adImageAddress(value) {
    if (typeof value !== 'string' || !value || value.length > MAX_ADDRESS || !/^https?:\/\//i.test(value)) return null;
    let url;
    try { url = new URL(value.replace(/^http:/i, 'https:')); }
    catch { return null; }
    if (url.protocol !== 'https:' || url.username || url.password || url.port) return null;
    // The browser never sends the fragment, so the page asks for the address without it.
    url.hash = '';
    return url.href;
}

export function imageType(body) {
    if (body.length >= 8 && body.subarray(0, 8).equals(Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]))) return 'image/png';
    if (body.length >= 3 && body[0] === 0xff && body[1] === 0xd8 && body[2] === 0xff) return 'image/jpeg';
    if (body.length >= 6 && /^GIF8[79]a$/.test(body.toString('latin1', 0, 6))) return 'image/gif';
    if (body.length >= 12 && body.toString('latin1', 0, 4) === 'RIFF' && body.toString('latin1', 8, 12) === 'WEBP') return 'image/webp';
    return null;
}

function publicLookup(hostname, options, callback) {
    lookup(hostname, { ...options, all: true }, (error, addresses) => {
        if (error) { callback(error); return; }
        if (!addresses.length || !addresses.every(entry => publicAddress(entry.address))) { callback(new Error(`Room ad host is not public: ${hostname}`)); return; }
        if (options.all) callback(null, addresses);
        else callback(null, addresses[0].address, addresses[0].family);
    });
}

function get(url, signal) {
    return new Promise((resolve, reject) => {
        const client = url.protocol === 'https:' ? https : http;
        const request = client.get(url, { lookup: publicLookup, signal, headers: { accept: 'image/*', 'user-agent': 'PlusEMU-Camera' } }, response => {
            if (response.statusCode >= 300 && response.statusCode < 400) {
                response.resume();
                resolve({ redirect: response.headers.location ?? '' });
                return;
            }
            if (response.statusCode !== 200) { response.resume(); reject(new Error(`Room ad image returned ${response.statusCode}`)); return; }
            if (Number(response.headers['content-length'] ?? 0) > MAX_IMAGE) { response.destroy(); reject(new Error('Room ad image is too large')); return; }
            const chunks = [];
            let length = 0;
            response.on('data', chunk => {
                length += chunk.length;
                if (length > MAX_IMAGE) { response.destroy(); reject(new Error('Room ad image is too large')); return; }
                chunks.push(chunk);
            });
            response.on('end', () => resolve({ body: Buffer.concat(chunks) }));
            response.on('error', reject);
        });
        request.on('error', reject);
    });
}

export async function fetchAdImage(href, signal) {
    const timeout = AbortSignal.timeout(DOWNLOAD_MS);
    const limit = signal ? AbortSignal.any([signal, timeout]) : timeout;
    let url = new URL(href);
    for (let hop = 0; hop <= MAX_REDIRECTS; hop++) {
        if (!['http:', 'https:'].includes(url.protocol) || url.username || url.password || url.port) throw new Error('Room ad address is not allowed');
        const host = url.hostname.replace(/^\[|\]$/g, '');
        if (host.toLowerCase() === 'localhost' || host.toLowerCase().endsWith('.localhost') || (isIP(host) && !publicAddress(host))) throw new Error(`Room ad host is not public: ${host}`);
        const result = await get(url, limit);
        if (result.body) return result.body;
        url = new URL(result.redirect, url);
    }
    throw new Error('Room ad image redirects too often');
}

// Serves only the room ads of scenes the service accepted, each for a while after its last scene.
export function createAdImages({ download = fetchAdImage, local, now = Date.now }) {
    const allowed = new Map();
    const cache = createAssetCache({
        load: async (href, signal) => {
            const body = (await local(href)) ?? await download(href, signal);
            if (!imageType(body)) throw new Error('Room ad is not an image');
            return { body, freshMs: KEEP_MS };
        },
        maxBytes: 64 * 1024 * 1024,
        maxDownloads: 4,
        now
    });
    const has = href => {
        const until = allowed.get(href);
        if (until === undefined) return false;
        if (until > now()) return true;
        allowed.delete(href);
        return false;
    };
    return {
        allow(scene) {
            for (const item of scene.items) {
                const href = adImageAddress(item?.branding?.imageUrl);
                if (!href) continue;
                allowed.delete(href);
                allowed.set(href, now() + KEEP_MS);
            }
            for (const href of allowed.keys()) {
                if (allowed.size <= 1000) break;
                allowed.delete(href);
            }
        },
        has,
        async load(href, signal) {
            if (!has(href)) throw new Error('Not a room ad image');
            const body = await cache(href, signal);
            return { body, contentType: imageType(body) };
        }
    };
}
