import { apiRequest } from '../api/client.js'
import { getDonation } from '../donations/api.js'
import { isGuid } from '../donations/format.js'
import { uniqueDonationIds } from './options.js'

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

// El negocio dueño de la donación (o el Admin) confirma la recogida; Pickups reserva la donación en Donations.
export function confirmPickup(id, signal) {
  if (!isGuid(id)) return Promise.reject(new Error('El identificador de la recogida no es válido.'))
  return apiRequest('/pickups/api/pickups/' + id + '/confirm', { method: 'PATCH', signal })
}

// El negocio dueño de la donación (o el Admin) completa la recogida; Pickups marca la donación como recogida.
export function completePickup(id, signal) {
  if (!isGuid(id)) return Promise.reject(new Error('El identificador de la recogida no es válido.'))
  return apiRequest('/pickups/api/pickups/' + id + '/complete', { method: 'PATCH', signal })
}

// Id de las donaciones que ya tienen una recogida pendiente o confirmada de cualquier fundación.
export function listActiveDonationIds(signal) {
  return apiRequest('/pickups/api/pickups/active-donations', { signal })
}

// Consulta en Donations cada donación de la página. Si una falla, esa recogida se muestra sin título ni estado.
export async function loadPickupDonations(pickups, signal) {
  const ids = uniqueDonationIds(pickups)
  const results = await Promise.allSettled(ids.map(id => getDonation(id, signal)))
  const found = {}
  results.forEach((result, index) => {
    if (result.status === 'fulfilled') found[ids[index]] = result.value
  })
  return found
}
