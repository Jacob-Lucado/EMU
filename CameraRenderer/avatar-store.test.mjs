import assert from 'node:assert/strict';
import { mkdir, mkdtemp, readdir, readFile, rm, stat, utimes, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import test from 'node:test';
import { createAvatarStore } from './avatar-store.mjs';

const image = (size, fill = 1) => Buffer.alloc(size, fill);
const exists = file => stat(file).then(() => true, () => false);

async function folder(t) {
    const root = await mkdtemp(join(tmpdir(), 'avatar-store-'));
    t.after(() => rm(root, { recursive: true, force: true }));
    return root;
}

async function files(root) {
    return (await readdir(root, { recursive: true, withFileTypes: true })).filter(entry => entry.isFile()).map(entry => join(entry.parentPath, entry.name)).sort();
}

async function place(root, file, size, seconds) {
    const target = join(root, file);
    await mkdir(dirname(target), { recursive: true });
    await writeFile(target, image(size));
    await utimes(target, seconds, seconds);
}

test('a saved image is stored at its path with no temporary file left', async t => {
    const root = await folder(t);
    const store = createAvatarStore({ root, maxBytes: 1000 });
    await store.open();
    await store.save('v1/hd-180-1/aaaa.m-m.png', image(100, 7));
    assert.deepEqual(await readFile(join(root, 'v1/hd-180-1/aaaa.m-m.png')), image(100, 7));
    assert.deepEqual(await files(root), [join(root, 'v1/hd-180-1/aaaa.m-m.png')]);
    assert.deepEqual(store.size(), { files: 1, bytes: 100 });
});

test('over the size limit the oldest images are deleted first, with their empty folders', async t => {
    const root = await folder(t);
    const store = createAvatarStore({ root, maxBytes: 250 });
    await store.open();
    await store.save('v1/hd-1/a.png', image(100));
    await store.save('v1/hd-2/b.png', image(100));
    await store.save('v1/hd-3/c.png', image(100));
    assert.deepEqual(await files(root), [join(root, 'v1/hd-2/b.png'), join(root, 'v1/hd-3/c.png')]);
    assert.equal(await exists(join(root, 'v1/hd-1')), false);
    assert.equal(await exists(join(root, 'v1')), true);
    assert.deepEqual(store.size(), { files: 2, bytes: 200 });
});

test('an image saved again is counted once', async t => {
    const root = await folder(t);
    const store = createAvatarStore({ root, maxBytes: 250 });
    await store.open();
    await store.save('v1/hd-1/a.png', image(100));
    await store.save('v1/hd-1/a.png', image(100));
    await store.save('v1/hd-2/b.png', image(100));
    assert.equal((await files(root)).length, 2);
    assert.deepEqual(store.size(), { files: 2, bytes: 200 });
});

test('many new images never take more than the size limit', async t => {
    const root = await folder(t);
    const store = createAvatarStore({ root, maxBytes: 1000 });
    await store.open();
    for (let index = 0; index < 50; index++) await store.save(`v1/hd-${index}/a.png`, image(100));
    const left = await files(root);
    assert.equal(left.length, 10);
    assert.deepEqual(left, Array.from({ length: 10 }, (_, index) => join(root, `v1/hd-${index + 40}/a.png`)).sort());
    assert.deepEqual(store.size(), { files: 10, bytes: 1000 });
});

test('images already stored count at start, oldest first, and left-over temporary files are deleted', async t => {
    const root = await folder(t);
    await place(root, 'v1/hd-1/new.png', 100, 3000);
    await place(root, 'v1/hd-2/old.png', 100, 1000);
    await place(root, 'v1/hd-3/mid.png', 100, 2000);
    await place(root, 'v1/hd-3/mid.png.1234.tmp', 100, 2000);
    const store = createAvatarStore({ root, maxBytes: 250 });
    await store.open();
    assert.deepEqual(await files(root), [join(root, 'v1/hd-1/new.png'), join(root, 'v1/hd-3/mid.png')]);
    assert.deepEqual(store.size(), { files: 2, bytes: 200 });
    await store.save('v1/hd-4/next.png', image(100));
    assert.deepEqual(await files(root), [join(root, 'v1/hd-1/new.png'), join(root, 'v1/hd-4/next.png')]);
});

test('an empty or missing folder starts empty', async t => {
    const root = join(await folder(t), 'avatars');
    const store = createAvatarStore({ root, maxBytes: 1000 });
    await store.open();
    assert.deepEqual(store.size(), { files: 0, bytes: 0 });
    await store.save('v1/hd-1/a.png', image(10));
    assert.deepEqual(store.size(), { files: 1, bytes: 10 });
});
