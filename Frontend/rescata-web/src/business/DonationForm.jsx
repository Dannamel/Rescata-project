import { useState } from 'react'
import { createDonation } from '../donations/api.js'
import {
  availabilityRange, emptyDonation, limits, quantityUnits,
  roadSuffixes, roadTypes, toDonationPayload, validateDonation,
} from './options.js'

export default function DonationForm({ businessId, categories, categoriesError, onCreated }) {
  const [fields, setFields] = useState(emptyDonation)
  const [errors, setErrors] = useState({})
  const [busy, setBusy] = useState(false)
  const [failure, setFailure] = useState('')
  const [published, setPublished] = useState('')
  const range = availabilityRange()

  function update(event) {
    const { name, value } = event.target
    setFields(current => ({ ...current, [name]: value }))
  }

  function fieldError(name) {
    return errors[name]
      ? <p className="error field-error" id={name + '-error'}>{errors[name]}</p>
      : null
  }

  function fieldProps(name) {
    return {
      id: name,
      name,
      value: fields[name],
      onChange: update,
      'aria-invalid': errors[name] ? true : undefined,
      'aria-describedby': errors[name] ? name + '-error' : undefined,
    }
  }

  async function submit(event) {
    event.preventDefault()
    if (busy) return

    const found = validateDonation(fields)
    setErrors(found)
    setFailure('')
    setPublished('')
    if (Object.keys(found).length > 0) return

    setBusy(true)
    try {
      await createDonation(toDonationPayload(fields, businessId))
      setFields(emptyDonation)
      setPublished('La donación quedó publicada.')
      onCreated()
    } catch (error) {
      setFailure(error.message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <form className="business-form" onSubmit={submit} noValidate>
      <h2>Publicar una donación</h2>

      <label htmlFor="title">Título</label>
      <input {...fieldProps('title')} type="text" maxLength={limits.titleMax} required />
      {fieldError('title')}

      <label htmlFor="description">Descripción</label>
      <textarea {...fieldProps('description')} maxLength={limits.descriptionMax} />
      {fieldError('description')}

      <div className="address-fields">
        <div>
          <label htmlFor="quantityAmount">Cantidad</label>
          <input {...fieldProps('quantityAmount')} type="number" min="0" step="0.01" required />
          {fieldError('quantityAmount')}
        </div>
        <div>
          <label htmlFor="quantityUnit">Unidad</label>
          <select {...fieldProps('quantityUnit')}>
            {quantityUnits.map(unit => <option key={unit} value={unit}>{unit}</option>)}
          </select>
          {fieldError('quantityUnit')}
        </div>
      </div>

      <label htmlFor="foodCategoryId">Categoría</label>
      <select {...fieldProps('foodCategoryId')} disabled={categories.length === 0} required>
        <option value="">Elige una categoría</option>
        {categories.map(category => <option key={category.id} value={category.id}>{category.name}</option>)}
      </select>
      {fieldError('foodCategoryId')}
      {categoriesError && <p className="error field-error">{categoriesError}</p>}

      <label htmlFor="availableUntil">Disponible hasta</label>
      <input {...fieldProps('availableUntil')} type="datetime-local" min={range.min} max={range.max} required />
      {fieldError('availableUntil')}

      <h3>Dirección de recogida</h3>
      <div className="address-fields">
        <div>
          <label htmlFor="roadType">Tipo de vía</label>
          <select {...fieldProps('roadType')}>
            {roadTypes.map(type => <option key={type} value={type}>{type}</option>)}
          </select>
          {fieldError('roadType')}
        </div>
        <div>
          <label htmlFor="roadNumber">Número de la vía</label>
          <input {...fieldProps('roadNumber')} type="text" maxLength={limits.roadNumberMax} required />
          {fieldError('roadNumber')}
        </div>
        <div>
          <label htmlFor="crossRoadNumber">Vía que cruza</label>
          <input {...fieldProps('crossRoadNumber')} type="text" required />
          {fieldError('crossRoadNumber')}
        </div>
        <div>
          <label htmlFor="plateNumber">Número de placa</label>
          <input {...fieldProps('plateNumber')} type="text" required />
          {fieldError('plateNumber')}
        </div>
        <div>
          <label htmlFor="roadSuffix">Sufijo</label>
          <select {...fieldProps('roadSuffix')}>
            {roadSuffixes.map(suffix => <option key={suffix} value={suffix}>{suffix}</option>)}
          </select>
          {fieldError('roadSuffix')}
        </div>
        <div>
          <label htmlFor="city">Ciudad</label>
          <input {...fieldProps('city')} type="text" required />
          {fieldError('city')}
        </div>
      </div>

      {failure && <p className="error" role="alert">{failure}</p>}
      {published && <p className="form-success" role="status">{published}</p>}

      <button disabled={busy}>{busy ? 'Publicando…' : 'Publicar donación'}</button>
    </form>
  )
}
