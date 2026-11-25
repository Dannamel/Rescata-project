import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { listDonations } from '../donations/api.js'
import { formatDate, formatQuantity } from '../donations/format.js'
import { cancelPickup, createPickup, listPickups, updatePickup } from '../pickups/api.js'
import { canCancelPickup, canReschedule, pickupFields, pickupStatusNames } from '../pickups/options.js'
import PickupForm from './PickupForm.jsx'

export default function FoundationPickups() {
  const [attempt, setAttempt] = useState(0)
  const [donations, setDonations] = useState({ state: 'loading' })
  const [pickups, setPickups] = useState({ state: 'loading' })
  const [page, setPage] = useState(1)
  const [open, setOpen] = useState('')
  const [reason, setReason] = useState('')
  const [message, setMessage] = useState('')
  const [actionError, setActionError] = useState('')

  useEffect(() => {
    const controller = new AbortController()
    setDonations({ state: 'loading' })
    listDonations({ page: 1, status: 'Available' }, controller.signal)
      .then(data => setDonations({ state: 'ready', data }))
      .catch(error => {
        if (error.name !== 'AbortError') setDonations({ state: 'error', message: error.message })
      })
    return () => controller.abort()
  }, [attempt])

  useEffect(() => {
    const controller = new AbortController()
    setPickups({ state: 'loading' })
    listPickups(page, controller.signal)
      .then(data => setPickups({ state: 'ready', data }))
      .catch(error => {
        if (error.name !== 'AbortError') setPickups({ state: 'error', message: error.message })
      })
    return () => controller.abort()
  }, [page, attempt])

  function done(text) {
    setOpen('')
    setReason('')
    setActionError('')
    setMessage(text)
    setAttempt(value => value + 1)
  }

  function toggle(key) {
    setMessage('')
    setActionError('')
    setReason('')
    setOpen(current => current === key ? '' : key)
  }

  async function schedule(donationId, payload) {
    await createPickup({ donationId, ...payload })
    done('La recogida quedó programada.')
  }

  async function reschedule(id, payload) {
    await updatePickup(id, payload)
    done('La recogida quedó reprogramada.')
  }

  async function cancel(id) {
    setActionError('')
    try {
      await cancelPickup(id, reason)
      done('La recogida quedó cancelada.')
    } catch (error) {
      setActionError(error.message)
    }
  }

  const totalPages = pickups.data?.totalPages ?? 0

  return <>
    <h1>Fundación</h1>
    {message && <p className="form-success" role="status">{message}</p>}

    <h2>Donaciones disponibles</h2>
    {donations.state === 'loading' && <p className="loading">Cargando donaciones…</p>}
    {donations.state === 'error' && <p className="error" role="alert">{donations.message}</p>}
    {donations.state === 'ready' && (donations.data.items.length === 0
      ? <p>No hay donaciones disponibles en este momento.</p>
      : <ul className="donation-list">
        {donations.data.items.map(donation => <li key={donation.id}>
          <div className="donation-heading">
            <Link to={'/donaciones/' + donation.id}>{donation.title}</Link>
          </div>
          <p className="donation-meta">
            <span>{donation.foodCategory || 'Sin categoría'}</span>
            <span>{formatQuantity(donation.quantityAmount, donation.quantityUnit)}</span>
            <span>Disponible hasta {formatDate(donation.availableUntil)}</span>
          </p>
          {open === 'new-' + donation.id
            ? <PickupForm availableUntil={donation.availableUntil} submitLabel="Programar recogida"
                onSubmit={payload => schedule(donation.id, payload)} onClose={() => toggle('')} />
            : <div className="donation-actions">
              <button type="button" onClick={() => toggle('new-' + donation.id)}>Programar recogida</button>
            </div>}
        </li>)}
      </ul>)}

    <h2>Mis recogidas</h2>
    {pickups.state === 'loading' && <p className="loading">Cargando recogidas…</p>}
    {pickups.state === 'error' && <p className="error" role="alert">{pickups.message}</p>}
    {pickups.state === 'ready' && (pickups.data.items.length === 0
      ? <p>Todavía no has programado recogidas.</p>
      : <>
        <ul className="donation-list">
          {pickups.data.items.map(pickup => <li key={pickup.id}>
            <div className="donation-heading">
              <strong>{formatDate(pickup.scheduledAt)}</strong>
              <span className={'status status-pickup-' + pickup.status.toLowerCase()}>
                {pickupStatusNames[pickup.status] ?? pickup.status}
              </span>
            </div>
            <p className="donation-meta">
              <span>{pickup.durationInMinutes} minutos</span>
              <Link to={'/donaciones/' + pickup.donationId}>Ver donación</Link>
              {pickup.notes && <span>{pickup.notes}</span>}
              {pickup.cancelReason && <span>Motivo: {pickup.cancelReason}</span>}
            </p>

            {open === 'edit-' + pickup.id &&
              <PickupForm initial={pickupFields(pickup)} submitLabel="Guardar cambios"
                onSubmit={payload => reschedule(pickup.id, payload)} onClose={() => toggle('')} />}

            {open === 'cancel-' + pickup.id && <div className="pickup-form">
              <label htmlFor="cancelReason">Motivo de la cancelación (opcional)</label>
              <textarea id="cancelReason" maxLength={512} value={reason} onChange={event => setReason(event.target.value)} />
              {actionError && <p className="error" role="alert">{actionError}</p>}
              <div className="donation-actions">
                <button type="button" onClick={() => cancel(pickup.id)}>Confirmar cancelación</button>
                <button type="button" className="secondary" onClick={() => toggle('')}>Cerrar</button>
              </div>
            </div>}

            {open !== 'edit-' + pickup.id && open !== 'cancel-' + pickup.id && <div className="donation-actions">
              {canReschedule(pickup.status) &&
                <button type="button" className="secondary" onClick={() => toggle('edit-' + pickup.id)}>Reprogramar</button>}
              {canCancelPickup(pickup.status) &&
                <button type="button" className="secondary" onClick={() => toggle('cancel-' + pickup.id)}>Cancelar recogida</button>}
            </div>}
          </li>)}
        </ul>
        {totalPages > 1 && <div className="pagination" role="navigation" aria-label="Paginación">
          <button type="button" className="secondary" disabled={page <= 1} onClick={() => setPage(page - 1)}>Anterior</button>
          <span aria-current="page">Página {Math.min(page, totalPages)} de {totalPages}</span>
          <button type="button" className="secondary" disabled={page >= totalPages} onClick={() => setPage(page + 1)}>Siguiente</button>
        </div>}
      </>)}
  </>
}
