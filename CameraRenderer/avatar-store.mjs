import { mkdir, readdir, rename, rmdir, stat, unlink, writeFile } from 'node:fs/promises';
import { dirname, join, relative, resolve, sep } from 'node:path';

// Keeps the stored avatar images under a size limit. Over the limit the oldest images go first; a deleted image is
// drawn again the next time the site asks for it.
export function createAvatarStore({ root, maxBytes }) {
    root = resolve(root);
    // Insertion order is age: the first entry is the oldest image.
    const stored = new Map();
    let bytes = 0;

    function forget(file) {
        bytes -= stored.get(file) ?? 0;
        stored.delete(file);
    }

    async function remove(file) {
        forget(file);
        await unlink(join(root, file)).catch(() => {});
        // Folders of figures without images go too, up to the root itself.
        for (let folder = dirname(join(root, file)); folder.startsWith(`${root}${sep}`); folder = dirname(folder)) {
            try { await rmdir(folder); }
            catch { break; }
        }
    }

    async function trim() {
        for (const file of stored.keys()) {
            if (bytes <= maxBytes || stored.size <= 1) break;
            await remove(file);
        }
    }

    function record(file, size) {
        forget(file);
        stored.set(file, size);
        bytes += size;
    }

    return {
        async open() {
            const found = [];
            const entries = await readdir(root, { recursive: true, withFileTypes: true }).catch(error => {
                if (error.code === 'ENOENT') return [];
                throw error;
            });
            for (const entry of entries) {
                if (!entry.isFile()) continue;
                const path = join(entry.parentPath, entry.name);
                // A temporary file is an image that was never finished.
                if (entry.name.endsWith('.tmp')) { await unlink(path).catch(() => {}); continue; }
                if (!entry.name.endsWith('.png')) continue;
                const info = await stat(path).catch(() => null);
                if (info) found.push({ file: relative(root, path), size: info.size, time: info.mtimeMs });
            }
            found.sort((a, b) => a.time - b.time);
            for (const { file, size } of found) record(file, size);
            await trim();
        },
        async save(file, image) {
            const target = resolve(root, file);
            await mkdir(dirname(target), { recursive: true });
            await writeFile(`${target}.${process.pid}.tmp`, image);
            await rename(`${target}.${process.pid}.tmp`, target);
            record(file, image.length);
            await trim();
        },
        size() {
            return { files: stored.size, bytes };
        }
    };
}
