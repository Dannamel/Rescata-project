import test from 'node:test'
import assert from 'node:assert/strict'
import { completePickup, confirmPickup } from '../src/pickups/api.js'
import { canComplete, canConfirm, uniqueDonationIds } from '../src/pickups/options.js'

test('el negocio solo confirma recogidas pendientes', () => {
  assert.equal(canConfirm('Pending'), true)
  assert.equal(canConfirm('Confirmed'), false)
  assert.equal(canConfirm('Cancelled'), false)
  assert.equal(canConfirm('Completed'), false)
})

test('el negocio solo completa recogidas confirmadas', () => {
  assert.equal(canComplete('Confirmed'), true)
  assert.equal(canComplete('Pending'), false)
  assert.equal(canComplete('Cancelled'), false)
  assert.equal(canComplete('Completed'), false)
})

test('cada donación se consulta una sola vez aunque tenga varias recogidas', () => {
  const pickups = [{ donationId: 'a' }, { donationId: 'b' }, { donationId: 'a' }]
  assert.deepEqual(uniqueDonationIds(pickups), ['a', 'b'])
})

test('no llama a Pickups para confirmar o completar con un identificador inválido', async () => {
  await assert.rejects(confirmPickup('abc'), /no es válido/)
  await assert.rejects(completePickup('abc'), /no es válido/)
})
