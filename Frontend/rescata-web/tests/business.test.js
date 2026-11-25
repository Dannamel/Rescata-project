import test from 'node:test'
import assert from 'node:assert/strict'
import { cancelDonation, donationsPath } from '../src/donations/api.js'
import {
  availabilityRange, canCancel, emptyDonation, toDonationPayload, toLocalInput, validateDonation,
} from '../src/business/options.js'

const negocio = '8f3a2b1c-4d5e-6f70-8192-a3b4c5d6e7f8'
const categoria = '0193a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a5b'
const ahora = new Date('2026-11-24T18:00:00Z')

function donacionValida(extra = {}) {
  return {
    ...emptyDonation,
    title: 'Pan del día',
    description: 'Pan francés horneado hoy',
    quantityAmount: '20',
    quantityUnit: 'Kilogramos',
    foodCategoryId: categoria,
    roadNumber: '10',
    crossRoadNumber: '43A',
    plateNumber: '25',
    roadSuffix: 'Sur',
    city: 'Medellín',
    availableUntil: toLocalInput(new Date(ahora.getTime() + 2 * 3600000)),
    ...extra,
  }
}

test('el listado del negocio pide solo sus donaciones y no altera las demás consultas', () => {
  assert.equal(donationsPath({ page: 1, businessId: negocio }),
    '/donations/api/donations?pageNumber=1&pageSize=10&businessId=' + negocio)
  assert.equal(donationsPath({ page: 1 }), '/donations/api/donations?pageNumber=1&pageSize=10')
})

test('una donación completa no reporta errores', () => {
  assert.deepEqual(validateDonation(donacionValida(), ahora), {})
})

test('aplica las mismas reglas que valida Donations', () => {
  assert.equal(validateDonation(donacionValida({ title: 'Pa' }), ahora).title,
    'El título debe tener entre 3 y 100 caracteres.')
  assert.equal(validateDonation(donacionValida({ title: 'x'.repeat(101) }), ahora).title,
    'El título debe tener entre 3 y 100 caracteres.')
  assert.equal(validateDonation(donacionValida({ description: 'x'.repeat(501) }), ahora).description,
    'La descripción no puede superar 500 caracteres.')
  assert.equal(validateDonation(donacionValida({ quantityAmount: '0' }), ahora).quantityAmount,
    'La cantidad debe ser mayor a cero.')
  assert.equal(validateDonation(donacionValida({ quantityAmount: '' }), ahora).quantityAmount,
    'La cantidad debe ser mayor a cero.')
  assert.equal(validateDonation(donacionValida({ roadNumber: 'x'.repeat(17) }), ahora).roadNumber,
    'El número de la vía no puede superar 16 caracteres.')
  assert.ok(validateDonation(donacionValida({ foodCategoryId: '' }), ahora).foodCategoryId)
  assert.ok(validateDonation(donacionValida({ foodCategoryId: 'panaderia' }), ahora).foodCategoryId)
  assert.ok(validateDonation(donacionValida({ crossRoadNumber: '  ' }), ahora).crossRoadNumber)
  assert.ok(validateDonation(donacionValida({ plateNumber: '' }), ahora).plateNumber)
  assert.ok(validateDonation(donacionValida({ city: '' }), ahora).city)
})

test('la hora límite debe caer entre 1 hora y 7 días a partir de ahora', () => {
  const mensaje = 'La hora límite debe estar entre 1 hora y 7 días a partir de ahora.'
  const en = minutos => toLocalInput(new Date(ahora.getTime() + minutos * 60000))

  assert.equal(validateDonation(donacionValida({ availableUntil: en(30) }), ahora).availableUntil, mensaje)
  assert.equal(validateDonation(donacionValida({ availableUntil: en(8 * 24 * 60) }), ahora).availableUntil, mensaje)
  assert.equal(validateDonation(donacionValida({ availableUntil: en(90) }), ahora).availableUntil, undefined)
  assert.ok(validateDonation(donacionValida({ availableUntil: '' }), ahora).availableUntil)
})

test('el rango del campo de fecha coincide con la ventana que acepta Donations', () => {
  const rango = availabilityRange(ahora)
  assert.equal(rango.min, toLocalInput(new Date(ahora.getTime() + 3600000)))
  assert.equal(rango.max, toLocalInput(new Date(ahora.getTime() + 7 * 24 * 3600000)))
  assert.match(rango.min, /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/)
})

test('arma el cuerpo que espera Donations con el negocio del token', () => {
  const fields = donacionValida({ title: '  Pan del día  ', city: ' Medellín ' })
  const payload = toDonationPayload(fields, negocio)

  assert.equal(payload.businessId, negocio)
  assert.equal(payload.title, 'Pan del día')
  assert.equal(payload.quantityAmount, 20)
  assert.equal(payload.quantityUnit, 'Kilogramos')
  assert.equal(payload.foodCategoryId, categoria)
  assert.deepEqual(payload.pickupAddress, {
    roadType: 'Calle',
    roadNumber: '10',
    crossRoadNumber: '43A',
    plateNumber: '25',
    roadSuffix: 'Sur',
    city: 'Medellín',
  })
  assert.match(payload.availableUntil, /Z$/)
  assert.equal(new Date(payload.availableUntil).getTime(), new Date(fields.availableUntil).getTime())
})

test('solo se puede cancelar una donación disponible', () => {
  assert.equal(canCancel('Available'), true)
  assert.equal(canCancel('Claimed'), false)
  assert.equal(canCancel('PickedUp'), false)
  assert.equal(canCancel('Cancelled'), false)
})

test('no intenta cancelar con un identificador mal formado', async t => {
  const fetch = t.mock.method(globalThis, 'fetch', async () => new Response(null, { status: 204 }))
  await assert.rejects(cancelDonation('123'), /no es válido/)
  assert.equal(fetch.mock.callCount(), 0)
})
