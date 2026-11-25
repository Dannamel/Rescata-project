import test from 'node:test'
import assert from 'node:assert/strict'
import { cancelPickup, pickupsPath, updatePickup } from '../src/pickups/api.js'
import {
  canCancelPickup, canReschedule, emptyPickup, pickupFields, toPickupPayload, validatePickup,
} from '../src/pickups/options.js'
import { toLocalInput } from '../src/business/options.js'

const ahora = new Date('2026-11-25T15:00:00Z')
const venceDonacion = '2026-11-25T20:00:00'

function recogida(extra = {}) {
  return {
    ...emptyPickup,
    scheduledAt: toLocalInput(new Date(ahora.getTime() + 2 * 3600000)),
    ...extra,
  }
}

test('el listado de recogidas pasa por el gateway con la paginación', () => {
  assert.equal(pickupsPath(2), '/pickups/api/pickups?pageNumber=2&pageSize=10')
})

test('una recogida dentro del plazo de la donación no reporta errores', () => {
  assert.deepEqual(validatePickup(recogida(), venceDonacion, ahora), {})
})

test('aplica las mismas reglas que valida Pickups', () => {
  const pasada = toLocalInput(new Date(ahora.getTime() - 3600000))
  assert.ok(validatePickup(recogida({ scheduledAt: '' }), venceDonacion, ahora).scheduledAt)
  assert.ok(validatePickup(recogida({ scheduledAt: pasada }), venceDonacion, ahora).scheduledAt)
  assert.ok(validatePickup(recogida({ durationInMinutes: '0' }), venceDonacion, ahora).durationInMinutes)
  assert.ok(validatePickup(recogida({ notes: 'x'.repeat(513) }), venceDonacion, ahora).notes)
})

test('la recogida debe terminar antes de que venza la donación', () => {
  const errores = validatePickup(recogida({ durationInMinutes: '400' }), venceDonacion, ahora)
  assert.equal(errores.scheduledAt, 'La recogida debe terminar antes de que venza la donación.')
})

test('el cuerpo enviado a Pickups lleva la fecha en UTC y sin notas vacías', () => {
  const payload = toPickupPayload(recogida({ notes: '   ' }))
  assert.ok(payload.scheduledAt.endsWith('Z'))
  assert.equal(payload.durationInMinutes, 30)
  assert.equal(payload.notes, null)
})

test('reprogramar parte de los datos actuales de la recogida', () => {
  const campos = pickupFields({ scheduledAt: '2026-11-25T17:00:00', durationInMinutes: 45, notes: null })
  assert.equal(campos.durationInMinutes, '45')
  assert.equal(campos.notes, '')
  assert.ok(campos.scheduledAt)
})

test('solo se reprograma una pendiente y se cancela una pendiente o confirmada', () => {
  assert.equal(canReschedule('Pending'), true)
  assert.equal(canReschedule('Confirmed'), false)
  assert.equal(canCancelPickup('Confirmed'), true)
  assert.equal(canCancelPickup('Completed'), false)
  assert.equal(canCancelPickup('Cancelled'), false)
})

test('no llama a Pickups con un identificador inválido', async () => {
  await assert.rejects(updatePickup('abc', {}), /no es válido/)
  await assert.rejects(cancelPickup('abc', ''), /no es válido/)
})
