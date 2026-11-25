import { useState } from 'react'
import { emptyPickup, notesMax, toPickupPayload, validatePickup } from '../pickups/options.js'

// Se usa para programar una recogida nueva y para reprogramar una pendiente.
export default function PickupForm({ initial = emptyPickup, availableUntil, submitLabel, onSubmit, onClose }) {
  const [fields, setFields] = useState(initial)
  const [errors, setErrors] = useState({})
  const [busy, setBusy] = useState(false)
  const [failure, setFailure] = useState('')

  function update(event) {
    const { name, value } = event.target
    setFields(current => ({ ...current, [name]: value }))
  }

  async function submit(event) {
    event.preventDefault()
    if (busy) return

    const found = validatePickup(fields, availableUntil)
    setErrors(found)
    setFailure('')
    if (Object.keys(found).length > 0) return

    setBusy(true)
    try {
      await onSubmit(toPickupPayload(fields))
    } catch (error) {
      setFailure(error.message)
      setBusy(false)
    }
  }

  return (
    <form className="pickup-form" onSubmit={submit} noValidate>
      <div className="address-fields">
        <div>
          <label htmlFor="scheduledAt">Fecha y hora</label>
          <input id="scheduledAt" name="scheduledAt" type="datetime-local" required
            value={fields.scheduledAt} onChange={update} aria-invalid={errors.scheduledAt ? true : undefined} />
          {errors.scheduledAt && <p className="error field-error">{errors.scheduledAt}</p>}
        </div>
        <div>
          <label htmlFor="durationInMinutes">Duración (minutos)</label>
          <input id="durationInMinutes" name="durationInMinutes" type="number" min="1" step="1" required
            value={fields.durationInMinutes} onChange={update} aria-invalid={errors.durationInMinutes ? true : undefined} />
          {errors.durationInMinutes && <p className="error field-error">{errors.durationInMinutes}</p>}
        </div>
      </div>

      <label htmlFor="notes">Notas</label>
      <textarea id="notes" name="notes" maxLength={notesMax} value={fields.notes} onChange={update} />
      {errors.notes && <p className="error field-error">{errors.notes}</p>}

      {failure && <p className="error" role="alert">{failure}</p>}

      <div className="donation-actions">
        <button disabled={busy}>{busy ? 'Guardando…' : submitLabel}</button>
        <button type="button" className="secondary" onClick={onClose} disabled={busy}>Cerrar</button>
      </div>
    </form>
  )
}
