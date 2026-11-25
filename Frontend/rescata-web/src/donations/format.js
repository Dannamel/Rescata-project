// Valores del enum DonationStatus de Donations y su nombre en la interfaz.
export const statusNames = {
  Available: 'Disponible',
  Claimed: 'Reclamada',
  PickedUp: 'Recogida',
  Cancelled: 'Cancelada',
  Expired: 'Vencida',
}

// Donations nunca asigna Expired, así que filtrar por él siempre daría una lista vacía.
export const filterableStatuses = ['Available', 'Claimed', 'PickedUp', 'Cancelled']

const unitNames = { Kilogramos: 'kg', Unidades: 'unidades', Litros: 'litros' }

const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

export function isGuid(value) {
  return typeof value === 'string' && guidPattern.test(value)
}

// Lee los filtros de la URL (?pagina=2&estado=Available&categoria=<guid>) descartando valores inválidos,
// porque Donations ignora un estado desconocido sin avisar y responde un 400 ante un Guid mal formado.
export function readFilters(params) {
  const page = Number.parseInt(params.get('pagina'), 10)
  const status = params.get('estado')
  const categoryId = params.get('categoria')
  return {
    page: Number.isInteger(page) && page > 0 ? page : 1,
    status: filterableStatuses.includes(status) ? status : '',
    categoryId: isGuid(categoryId) ? categoryId : '',
  }
}

export function writeFilters({ page = 1, status = '', categoryId = '' }) {
  const params = new URLSearchParams()
  if (page > 1) params.set('pagina', String(page))
  if (status) params.set('estado', status)
  if (categoryId) params.set('categoria', categoryId)
  return params
}

// Donations guarda las fechas en UTC pero las devuelve sin zona ("2026-11-24T15:00:00");
// sin la Z el navegador las tomaría como hora local.
export function parseUtc(value) {
  if (typeof value !== 'string' || !value) return null
  const date = new Date(/(Z|[+-]\d{2}:?\d{2})$/i.test(value) ? value : value + 'Z')
  return Number.isNaN(date.getTime()) ? null : date
}

const dateFormat = new Intl.DateTimeFormat('es-CO', { dateStyle: 'medium', timeStyle: 'short' })
const numberFormat = new Intl.NumberFormat('es-CO', { maximumFractionDigits: 2 })

export function formatDate(value) {
  const date = parseUtc(value)
  return date ? dateFormat.format(date) : 'Sin fecha'
}

export function formatQuantity(amount, unit) {
  const number = typeof amount === 'number' ? numberFormat.format(amount) : amount
  return number + ' ' + (unitNames[unit] ?? unit)
}

export function statusName(status) {
  return statusNames[status] ?? status
}
