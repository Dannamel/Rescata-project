import { decodeSession, endSession, getToken } from '../auth/session.js'

const gateway = (import.meta.env?.VITE_GATEWAY_URL || 'http://localhost:5000').replace(/\/+$/, '')

export async function apiRequest(path, { method = 'GET', body, signal, authenticated = true } = {}) {
  if (!/^\/(identity|donations|pickups)\//.test(path)) throw new Error('Ruta de API inválida.')
  const headers = { Accept: 'application/json' }
  const token = authenticated ? getToken() : null
  if (authenticated) {
    if (!decodeSession(token)) {
      endSession(token)
      throw new Error('Inicia sesión para continuar.')
    }
    headers.Authorization = 'Bearer ' + token
  }
  if (body !== undefined) headers['Content-Type'] = 'application/json'

  let response
  try {
    response = await fetch(gateway + path, { method, headers, body: JSON.stringify(body), signal })
  } catch (error) {
    if (error.name === 'AbortError') throw error
    throw new Error('No se pudo conectar con el gateway. Vuelve a intentarlo.')
  }
  if (response.status === 401) {
    endSession(token)
    throw new Error(authenticated ? 'La sesión terminó.' : 'Correo o contraseña incorrectos.')
  }
  if (response.status === 403) throw new Error('Tu cuenta no tiene permiso para esta acción.')
  if (!response.ok) throw new Error(await describeFailure(response))
  if (response.status === 204) return null
  return response.json()
}

async function describeFailure(response) {
  const fallback = 'El servicio no pudo completar la solicitud (' + response.status + ').'
  try {
    const body = await response.json()
    const details = Array.isArray(body?.errors) ? body.errors.filter(detail => typeof detail === 'string') : []
    if (details.length > 0) return details.join(' ')
    if (typeof body?.message === 'string' && body.message) return body.message
    return fallback
  } catch {
    return fallback
  }
}