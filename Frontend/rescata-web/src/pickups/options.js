import { parseUtc } from '../donations/format.js'
import { toLocalInput } from '../business/options.js'

// Valores del enum PickupStatusEnum de Pickups y su nombre en la interfaz.
export const pickupStatusNames = {
  Pending: 'Pendiente',
  Confirmed: 'Confirmada',
  Cancelled: 'Cancelada',
  Completed: 'Completada',
}

export const notesMax = 512

export const emptyPickup = { scheduledAt: '', durationInMinutes: '30', notes: '' }

export function pickupFields(pickup) {
  const scheduledAt = parseUtc(pickup.scheduledAt)
  return {
    scheduledAt: scheduledAt ? toLocalInput(scheduledAt) : '',
    durationInMinutes: String(pickup.durationInMinutes),
    notes: pickup.notes ?? '',
  }
}

// Mismas reglas que valida Pickups: fecha futura, duración positiva y que termine antes de que venza la donación.
export function validatePickup(fields, availableUntil, now = new Date()) {
  const errors = {}
  const scheduledAt = fields.scheduledAt ? new Date(fields.scheduledAt) : null
  const duration = Number(fields.durationInMinutes)

  if (!scheduledAt || Number.isNaN(scheduledAt.getTime())) {
    errors.scheduledAt = 'Indica la fecha y hora de la recogida.'
  } else if (scheduledAt <= now) {
    errors.scheduledAt = 'La fecha de recogida debe ser futura.'
  }

  if (!Number.isInteger(duration) || duration <= 0) {
    errors.durationInMinutes = 'La duración debe ser un número entero mayor a cero.'
  }

  const deadline = parseUtc(availableUntil)
  if (!errors.scheduledAt && !errors.durationInMinutes && deadline &&
      scheduledAt.getTime() + duration * 60000 > deadline.getTime()) {
    errors.scheduledAt = 'La recogida debe terminar antes de que venza la donación.'
  }

  if (fields.notes.trim().length > notesMax) {
    errors.notes = 'Las notas no pueden superar 512 caracteres.'
  }

  return errors
}

export function toPickupPayload(fields) {
  return {
    scheduledAt: new Date(fields.scheduledAt).toISOString(),
    durationInMinutes: Number(fields.durationInMinutes),
    notes: fields.notes.trim() || null,
  }
}

export function canReschedule(status) {
  return status === 'Pending'
}

export function canCancelPickup(status) {
  return status === 'Pending' || status === 'Confirmed'
}

export function canConfirm(status) {
  return status === 'Pending'
}

export function canComplete(status) {
  return status === 'Confirmed'
}

// Id de las donaciones de una página de recogidas, sin repetir, para consultar cada donación una sola vez.
export function uniqueDonationIds(pickups) {
  return [...new Set(pickups.map(pickup => pickup.donationId))]
}
