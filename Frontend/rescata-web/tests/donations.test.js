import test from 'node:test'
import assert from 'node:assert/strict'
import { donationsPath, getDonation } from '../src/donations/api.js'
import { filterableStatuses, formatQuantity, parseUtc, readFilters, statusName, writeFilters } from '../src/donations/format.js'

const category = '0193a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a5b'

test('arma la consulta de Donations solo con los filtros presentes', () => {
  assert.equal(donationsPath({ page: 1 }), '/donations/api/donations?pageNumber=1&pageSize=10')
  assert.equal(donationsPath({ page: 3, status: 'Claimed', categoryId: category }),
    '/donations/api/donations?pageNumber=3&pageSize=10&status=Claimed&foodCategoryId=' + category)
})

test('descarta de la URL los filtros que Donations ignoraría o rechazaría', () => {
  assert.deepEqual(readFilters(new URLSearchParams('pagina=2&estado=Available&categoria=' + category)),
    { page: 2, status: 'Available', categoryId: category })
  assert.deepEqual(readFilters(new URLSearchParams('pagina=-4&estado=available&categoria=panaderia')),
    { page: 1, status: '', categoryId: '' })
  assert.deepEqual(readFilters(new URLSearchParams('estado=Expired')).status, '')
  assert.equal(filterableStatuses.includes('Expired'), false)
})

test('la URL de filtros omite los valores por defecto', () => {
  assert.equal(writeFilters({ page: 1, status: '', categoryId: '' }).toString(), '')
  assert.equal(writeFilters({ page: 2, status: 'PickedUp', categoryId: category }).toString(),
    'pagina=2&estado=PickedUp&categoria=' + category)
})

test('interpreta como UTC las fechas que Donations devuelve sin zona', () => {
  assert.equal(parseUtc('2026-11-24T15:00:00').toISOString(), '2026-11-24T15:00:00.000Z')
  assert.equal(parseUtc('2026-11-24T15:00:00.1234567').toISOString(), '2026-11-24T15:00:00.123Z')
  assert.equal(parseUtc('2026-11-24T10:00:00-05:00').toISOString(), '2026-11-24T15:00:00.000Z')
  assert.equal(parseUtc('no es fecha'), null)
  assert.equal(parseUtc(undefined), null)
})

test('traduce estados y unidades sin perder valores desconocidos', () => {
  assert.equal(statusName('Available'), 'Disponible')
  assert.equal(statusName('Expired'), 'Vencida')
  assert.equal(statusName('Otro'), 'Otro')
  assert.equal(formatQuantity(2.5, 'Kilogramos'), '2,5 kg')
  assert.equal(formatQuantity(3, 'Cajas'), '3 Cajas')
})

test('no consulta el detalle con un identificador mal formado', async t => {
  const fetch = t.mock.method(globalThis, 'fetch', async () => new Response('{}'))
  await assert.rejects(getDonation('123'), /no es válido/)
  assert.equal(fetch.mock.callCount(), 0)
})
