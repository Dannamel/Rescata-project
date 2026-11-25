import { apiRequest } from '../api/client.js'
import { isGuid } from './format.js'

export const pageSize = 10

// GET /api/donations de Donations, a través del gateway.
export function donationsPath({ page = 1, status = '', categoryId = '', businessId = '' } = {}) {
  const query = new URLSearchParams({ pageNumber: String(page), pageSize: String(pageSize) })
  if (status) query.set('status', status)
  if (categoryId) query.set('foodCategoryId', categoryId)
  if (businessId) query.set('businessId', businessId)
  return '/donations/api/donations?' + query
}

// Responde { items, totalCount, pageNumber, pageSize, totalPages }.
export function listDonations(filters, signal) {
  return apiRequest(donationsPath(filters), { signal })
}

export function getDonation(id, signal) {
  if (!isGuid(id)) return Promise.reject(new Error('El identificador de la donación no es válido.'))
  return apiRequest('/donations/api/donations/' + id, { signal })
}

// Responde [{ id, name }].
export function listFoodCategories(signal) {
  return apiRequest('/donations/api/food-categories', { signal })
}

export function createDonation(donation, signal) {
  return apiRequest('/donations/api/donations', { method: 'POST', body: donation, signal })
}

export function cancelDonation(id, signal) {
  if (!isGuid(id)) return Promise.reject(new Error('El identificador de la donación no es válido.'))
  return apiRequest('/donations/api/donations/' + id + '/cancel', { method: 'PATCH', signal })
}