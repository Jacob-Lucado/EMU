import assert from 'node:assert/strict';
import http from 'node:http';
import test from 'node:test';
import { adImageAddress, createAdImages, fetchAdImage, imageType, publicAddress } from './ad-images.mjs';

const PNG = Buffer.from('89504e470d0a1a0a0000000d49484452000000010000000108060000001f15c4890000000d4944415478da63f8ffff3f0005fe02fe0dc8a5d20000000049454e44ae426082', 'hex');

test('room ad images use plain web addresses, loaded over https like the client does', () => {
    assert.equal(adImageAddress('http://images.example/ads/hotel.png'), 'https://images.example/ads/hotel.png');
    assert.equal(adImageAddress('https://images.example/a b.png'), 'https://images.example/a%20b.png');
    assert.equal(adImageAddress('https://images.example/a.png?v=2#top'), 'https://images.example/a.png?v=2');
    for (const bad of ['', 'javascript:alert(1)', 'data:image/png;base64,AAAA', 'file:///etc/passwd', 'ftp://images.example/a.png',
        '//images.example/a.png', '/camera/a.png', 'https://user:secret@images.example/a.png', 'https://images.example:8443/a.png',
        'http://images.example:80/a.png', 'https:/images.example/a.png',
        `https://images.example/${'a'.repeat(500)}.png`, 42, null]) assert.equal(adImageAddress(bad), null, String(bad));
});

test('only public addresses are downloaded', () => {
    for (const address of ['8.8.8.8', '1.1.1.1', '2606:4700:4700::1111']) assert.equal(publicAddress(address), true, address);
    for (const address of ['127.0.0.1', '10.1.2.3', '172.16.0.1', '192.168.1.1', '169.254.169.254', '100.64.0.1', '0.0.0.0', '224.0.0.1',
        '::1', '::', 'fe80::1', 'fd00::1', '::ffff:127.0.0.1', '::ffff:7f00:1', '64:ff9b::a00:1', '2002:a00:1::1', 'localhost', '']) assert.equal(publicAddress(address), false, address);
});

test('a room ad must be a real image', () => {
    assert.equal(imageType(PNG), 'image/png');
    assert.equal(imageType(Buffer.from('ffd8ffe000104a464946', 'hex')), 'image/jpeg');
    assert.equal(imageType(Buffer.from('GIF89a\x01\x00', 'latin1')), 'image/gif');
    assert.equal(imageType(Buffer.from('RIFF\x10\x00\x00\x00WEBPVP8 ', 'latin1')), 'image/webp');
    assert.equal(imageType(Buffer.from('<svg xmlns="http://www.w3.org/2000/svg"/>')), null);
    assert.equal(imageType(Buffer.from('<html></html>')), null);
});

test('a download never reaches a private address, also after a redirect', async t => {
    let hits = 0;
    const server = http.createServer((request, response) => { hits++; response.writeHead(302, { location: 'http://127.0.0.1/secret.png' }).end(); });
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    t.after(() => server.close());
    await assert.rejects(fetchAdImage(`http://127.0.0.1:${server.address().port}/a.png`), /not public|address/i);
    await assert.rejects(fetchAdImage('https://localhost/a.png'), /not public/i);
    assert.equal(hits, 0);
});

test('only the room ads of a scene the service accepted are served', async () => {
    const downloads = [];
    const ads = createAdImages({
        download: async href => { downloads.push(href); return PNG; },
        local: async href => href.startsWith('https://hotel.example/c_images/') ? PNG : null
    });
    const scene = { items: [
        { id: 1, branding: { imageUrl: 'http://images.example/ad.png' } },
        { id: 2, branding: { imageUrl: 'https://hotel.example/c_images/ads/own.png' } },
        { id: 3, extraData: '1' }
    ] };
    assert.equal(ads.has('https://images.example/ad.png'), false);
    ads.allow(scene);
    assert.equal(ads.has('https://images.example/ad.png'), true);
    assert.equal(ads.has('http://images.example/ad.png'), false);
    assert.equal(ads.has('https://images.example/other.png'), false);
    assert.deepEqual(await ads.load('https://images.example/ad.png'), { body: PNG, contentType: 'image/png' });
    assert.deepEqual(await ads.load('https://hotel.example/c_images/ads/own.png'), { body: PNG, contentType: 'image/png' });
    assert.deepEqual(downloads, ['https://images.example/ad.png']);
    await assert.rejects(ads.load('https://images.example/other.png'), /not a room ad/i);
});

test('a download that is not an image is refused', async () => {
    const ads = createAdImages({ download: async () => Buffer.from('<html></html>'), local: async () => null });
    ads.allow({ items: [{ branding: { imageUrl: 'https://images.example/page.png' } }] });
    await assert.rejects(ads.load('https://images.example/page.png'), /not an image/i);
});
