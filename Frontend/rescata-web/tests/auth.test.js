import test from 'node:test'
import assert from 'node:assert/strict'
import { decodeSession, getToken, startSession } from '../src/auth/session.js'
import { areas, canAccess } from '../src/auth/access.js'
import { apiRequest } from '../src/api/client.js'

function tokenFor(roles, exp = Math.floor(Date.now() / 1000) + 60) {
  const payload = {
    sub: 'usuario', name: 'fundación@ejemplo.local', exp,
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': roles,
  }
  return 'header.' + Buffer.from(JSON.stringify(payload)).toString('base64url') + '.signature'
}

test('lee el claim de Identity, conserva varios roles y descarta sesiones vencidas', () => {
  const token = tokenFor(['Foundation', 'Business'])
  const session = decodeSession(token)
  assert.equal(session.email, 'fundación@ejemplo.local')
  assert.deepEqual(areas.filter(area => canAccess(area, session)).map(area => area.path), ['/negocio', '/fundacion'])
  assert.equal(decodeSession(token, session.expiresAt), null)
  assert.equal(decodeSession('dato-corrupto'), null)
})

test('un 401 invalida solo el token de la petición que lo recibió', async t => {
  const store = new Map()
  t.mock.method(globalThis, 'fetch', async () => new Response(null, { status: 401 }))
  Object.defineProperty(globalThis, 'sessionStorage', { configurable: true, value: {
    getItem: key => store.get(key) ?? null,
    setItem: (key, value) => store.set(key, value),
    removeItem: key => store.delete(key),
  } })
  globalThis.window = new EventTarget()
  t.after(() => { delete globalThis.window; delete globalThis.sessionStorage })
  const first = tokenFor('Business')
  const next = tokenFor('Admin')
  startSession(first)
  const pending = apiRequest('/identity/api/users')
  startSession(next)
  await assert.rejects(pending, /sesión terminó/)
  assert.equal(getToken(), next)
  await assert.rejects(apiRequest('/identity/api/users'), /sesión terminó/)
  assert.equal(getToken(), null)
})
