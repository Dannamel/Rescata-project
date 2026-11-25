import { isGuid } from '../donations/format.js'

export const quantityUnits = ['Kilogramos', 'Unidades', 'Litros']

export const roadTypes = ['Calle', 'Carrera', 'Avenida', 'Diagonal', 'Transversal', 'Circular']

export const roadSuffixes = ['Ninguno', 'Norte', 'Sur', 'Este', 'Oeste']

export const limits = {
  titleMin: 3,
  titleMax: 100,
  descriptionMax: 500,
  roadNumberMax: 16,
  minHours: 1,
  maxDays: 7,
}

const hour = 3600000
const day = 24 * hour

export const emptyDonation = {
  title: '',
  description: '',
  quantityAmount: '',
  quantityUnit: 'Kilogramos',
  foodCategoryId: '',
  roadType: 'Calle',
  roadNumber: '',
  crossRoadNumber: '',
  plateNumber: '',
  roadSuffix: 'Ninguno',
  city: '',
  availableUntil: '',
}

export function toLocalInput(date) {
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000)
  return local.toISOString().slice(0, 16)
}

export function availabilityRange(now = new Date()) {
  return {
    min: toLocalInput(new Date(now.getTime() + limits.minHours * hour)),
    max: toLocalInput(new Date(now.getTime() + limits.maxDays * day)),
  }
}

export function validateDonation(fields, now = new Date()) {
  const errors = {}
  const title = fields.title.trim()

  if (title.length < limits.titleMin || title.length > limits.titleMax) {
    errors.title = 'El título debe tener entre 3 y 100 caracteres.'
  }
  if (fields.description.trim().length > limits.descriptionMax) {
    errors.description = 'La descripción no puede superar 500 caracteres.'
  }

  const amount = Number(fields.quantityAmount)
  if (fields.quantityAmount === '' || !Number.isFinite(amount) || amount <= 0) {
    errors.quantityAmount = 'La cantidad debe ser mayor a cero.'
  }
  if (!quantityUnits.includes(fields.quantityUnit)) {
    errors.quantityUnit = 'Elige una unidad de medida.'
  }
  if (!isGuid(fields.foodCategoryId)) {
    errors.foodCategoryId = 'Elige una categoría.'
  }

  if (!roadTypes.includes(fields.roadType)) {
    errors.roadType = 'Elige un tipo de vía.'
  }
  const roadNumber = fields.roadNumber.trim()
  if (!roadNumber) {
    errors.roadNumber = 'El número de la vía es obligatorio.'
  } else if (roadNumber.length > limits.roadNumberMax) {
    errors.roadNumber = 'El número de la vía no puede superar 16 caracteres.'
  }
  if (!fields.crossRoadNumber.trim()) {
    errors.crossRoadNumber = 'El número de la vía que cruza es obligatorio.'
  }
  if (!fields.plateNumber.trim()) {
    errors.plateNumber = 'El número de placa es obligatorio.'
  }
  if (!roadSuffixes.includes(fields.roadSuffix)) {
    errors.roadSuffix = 'Elige un sufijo de vía.'
  }
  if (!fields.city.trim()) {
    errors.city = 'La ciudad es obligatoria.'
  }

  const availableUntil = fields.availableUntil ? new Date(fields.availableUntil) : null
  if (!availableUntil || Number.isNaN(availableUntil.getTime())) {
    errors.availableUntil = 'Indica hasta cuándo está disponible la donación.'
  } else if (availableUntil.getTime() < now.getTime() + limits.minHours * hour ||
             availableUntil.getTime() > now.getTime() + limits.maxDays * day) {
    errors.availableUntil = 'La hora límite debe estar entre 1 hora y 7 días a partir de ahora.'
  }

  return errors
}

export function toDonationPayload(fields, businessId) {
  return {
    businessId,
    title: fields.title.trim(),
    description: fields.description.trim(),
    quantityAmount: Number(fields.quantityAmount),
    quantityUnit: fields.quantityUnit,
    foodCategoryId: fields.foodCategoryId,
    pickupAddress: {
      roadType: fields.roadType,
      roadNumber: fields.roadNumber.trim(),
      crossRoadNumber: fields.crossRoadNumber.trim(),
      plateNumber: fields.plateNumber.trim(),
      roadSuffix: fields.roadSuffix,
      city: fields.city.trim(),
    },
    availableUntil: new Date(fields.availableUntil).toISOString(),
  }
}

export function canCancel(status) {
  return status === 'Available'
}
