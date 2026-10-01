import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import ts from 'typescript';

const source = await readFile(new URL('../src/utils/crypto.ts', import.meta.url), 'utf8');
const compiled = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022 } });
const crypto = await import(`data:text/javascript;base64,${Buffer.from(compiled.outputText).toString('base64')}`);

test('file encryption round trips empty, Unicode, and binary contents', async () => {
    const key = await crypto.deriveKey('test-only password', 'test-user');
    for (const bytes of [new Uint8Array(), new TextEncoder().encode('Vault test: සිංහල 日本語'), Uint8Array.from({ length: 4096 }, (_, i) => i % 256)]) {
        const encrypted = await crypto.encryptData(bytes, key);
        assert.equal(encrypted.iv.length, 12);
        assert.equal(encrypted.authTag.length, 16);
        assert.deepEqual(await crypto.decryptData(encrypted.payload, encrypted.iv, encrypted.authTag, key), bytes);
        assert.deepEqual(crypto.base64ToArrayBuffer(crypto.arrayBufferToBase64(bytes)), bytes);
    }
});

test('wrong passwords and tampered content fail authentication', async () => {
    const key = await crypto.deriveKey('correct password', 'test-user');
    const wrongKey = await crypto.deriveKey('wrong password', 'test-user');
    const encrypted = await crypto.encryptData(new Uint8Array([1, 2, 3]), key);
    await assert.rejects(crypto.decryptData(encrypted.payload, encrypted.iv, encrypted.authTag, wrongKey));
    encrypted.payload[0] ^= 1;
    await assert.rejects(crypto.decryptData(encrypted.payload, encrypted.iv, encrypted.authTag, key));
});

test('repeated encryption uses different nonces', async () => {
    const key = await crypto.deriveKey('test password', 'test-user');
    const first = await crypto.encryptData(new Uint8Array([1]), key);
    const second = await crypto.encryptData(new Uint8Array([1]), key);
    assert.notDeepEqual(first.iv, second.iv);
});
