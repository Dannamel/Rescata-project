const storageKey = 'rescata.accessToken'
const changed = 'rescata:session'
const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

export function decodeSession(token, now = Date.now()) {
  if (typeof token !== 'string') return null
  const parts = token.split('.')
  if (parts.length !== 3 || parts.some(part => !part)) return null

  try {
    const binary = atob(parts[1].replaceAll('-', '+').replaceAll('_', '/'))
    const bytes = Uint8Array.from(binary, char => char.charCodeAt(0))
    const claims = JSON.parse(new TextDecoder().decode(bytes))
    const expiresAt = claims.exp * 1000
    if (!claims.sub || !Number.isFinite(claims.exp) || expiresAt <= now) return null
    const roles = claims[roleClaim] ?? claims.role ?? []
    return {
      userId: claims.sub,
      email: claims.name,
      roles: (Array.isArray(roles) ? roles : [roles]).filter(role => typeof role === 'string'),
      expiresAt,
    }
  } catch {
    return null
  }
}

export function getToken() {
  return sessionStorage.getItem(storageKey)
}

export function startSession(token) {
  if (!decodeSession(token)) throw new Error('Identity devolvió una sesión inválida.')
  sessionStorage.setItem(storageKey, token)
  window.dispatchEvent(new Event(changed))
}

export function endSession(token = getToken()) {
  if (token && token === getToken()) {
    sessionStorage.removeItem(storageKey)
    window.dispatchEvent(new Event(changed))
  }
}

export function subscribeSession(listener) {
  window.addEventListener(changed, listener)
  return () => window.removeEventListener(changed, listener)
}
