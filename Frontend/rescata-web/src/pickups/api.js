import { apiRequest } from '../api/client.js'
import { isGuid } from '../donations/format.js'

export const pageSize = 10

// GET /api/pickups de Pickups, a través del gateway. La fundación solo recibe sus propias recogidas.
export function pickupsPath(page = 1) {
  const query = new URLSearchParams({ pageNumber: String(page), pageSize: String(pageSize) })
  return '/pickups/api/pickups?' + query
}

export function listPickups(page, signal) {
  return apiRequest(pickupsPath(page), { signal })
}

export function createPickup(pickup, signal) {
  return apiRequest('/pickups/api/pickups', { method: 'POST', body: pickup, signal })
}

export function updatePickup(id, pickup, signal) {
  if (!isGuid(id)) return Promise.reject(new Error('El identificador de la recogida no es válido.'))
  return apiRequest('/pickups/api/pickups/' + id, { method: 'PUT', body: pickup, signal })
}

export function cancelPickup(id, cancelReason, signal) {
  if (!isGuid(id)) return Promise.reject(new Error('El identificador de la recogida no es válido.'))
  return apiRequest('/pickups/api/pickups/' + id + '/cancel', {
    method: 'PATCH',
    body: { cancelReason: cancelReason.trim() || null },
    signal,
  })
}
