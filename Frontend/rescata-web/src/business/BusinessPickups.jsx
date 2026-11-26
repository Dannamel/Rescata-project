import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { formatDate } from '../donations/format.js'
import StatusBadge from '../donations/StatusBadge.jsx'
import { completePickup, confirmPickup, listPickups, loadPickupDonations } from '../pickups/api.js'
import { canComplete, canConfirm, pickupStatusNames, uniqueDonationIds } from '../pickups/options.js'

// Recogidas de las donaciones del negocio. Al confirmar o completar, Pickups le pide a Donations que reserve
// la donación (Claimed) o la marque como recogida (PickedUp); por eso se vuelve a consultar cada donación
// y se muestra su estado junto al de la recogida.
export default function BusinessPickups({ onChanged }) {
  const [page, setPage] = useState(1)
  const [attempt, setAttempt] = useState(0)
  const [result, setResult] = useState({ state: 'loading' })
  const [donations, setDonations] = useState({})
  const [working, setWorking] = useState('')
  const [actionError, setActionError] = useState('')
  const [message, setMessage] = useState('')

  useEffect(() => {
    const controller = new AbortController()
    setResult({ state: 'loading' })
    listPickups(page, controller.signal)
      .then(async data => {
        const found = await loadPickupDonations(data.items, controller.signal)
        if (controller.signal.aborted) return
        setDonations(found)
        setResult({ state: 'ready', data })
      })
      .catch(error => {
        if (error.name !== 'AbortError') setResult({ state: 'error', message: error.message })
      })
    return () => controller.abort()
  }, [page, attempt])

  async function run(pickup, action) {
    if (working) return
    setWorking(pickup.id)
    setActionError('')
    setMessage('')
    try {
      if (action === 'confirm') {
        await confirmPickup(pickup.id)
        setMessage('La recogida quedó confirmada y la donación quedó reservada.')
      } else {
        await completePickup(pickup.id)
        setMessage('La recogida quedó completada y la donación quedó marcada como recogida.')
      }
      setAttempt(value => value + 1)
      onChanged?.()
    } catch (error) {
      setActionError(error.message)
    } finally {
      setWorking('')
    }
  }

  const data = result.state === 'ready' ? result.data : null
  const totalPages = data?.totalPages ?? 0

  return <>
    <h2>Recogidas de mis donaciones</h2>

    {message && <p className="form-success" role="status">{message}</p>}
    {actionError && <p className="error" role="alert">{actionError}</p>}
    {result.state === 'loading' && <p className="loading">Cargando recogidas…</p>}

    {result.state === 'error' && <div role="alert">
      <p className="error">{result.message}</p>
      <button type="button" onClick={() => setAttempt(value => value + 1)}>Reintentar</button>
    </div>}

    {data && (data.items.length === 0
      ? <p>Todavía no hay recogidas programadas para tus donaciones.</p>
      : <>
        <ul className="donation-list">
          {data.items.map(pickup => {
            const donation = donations[pickup.donationId]
            return <li key={pickup.id}>
              <div className="donation-heading">
                <Link to={'/donaciones/' + pickup.donationId}>{donation?.title ?? 'Ver donación'}</Link>
                <span className={'status status-pickup-' + pickup.status.toLowerCase()}>
                  {pickupStatusNames[pickup.status] ?? pickup.status}
                </span>
              </div>
              <p className="donation-meta">
                <span>Recogida: {formatDate(pickup.scheduledAt)}</span>
                <span>{pickup.durationInMinutes} minutos</span>
                {donation && <span>Donación: <StatusBadge status={donation.status} /></span>}
                {pickup.notes && <span>{pickup.notes}</span>}
                {pickup.cancelReason && <span>Motivo: {pickup.cancelReason}</span>}
              </p>
              {(canConfirm(pickup.status) || canComplete(pickup.status)) && <div className="donation-actions">
                {canConfirm(pickup.status) &&
                  <button type="button" disabled={working !== ''} onClick={() => run(pickup, 'confirm')}>
                    {working === pickup.id ? 'Confirmando…' : 'Confirmar recogida'}
                  </button>}
                {canComplete(pickup.status) &&
                  <button type="button" disabled={working !== ''} onClick={() => run(pickup, 'complete')}>
                    {working === pickup.id ? 'Completando…' : 'Marcar como completada'}
                  </button>}
              </div>}
            </li>
          })}
        </ul>
        {totalPages > 1 && <div className="pagination" role="navigation" aria-label="Paginación">
          <button type="button" className="secondary" disabled={page <= 1}
            onClick={() => setPage(page - 1)}>Anterior</button>
          <span aria-current="page">Página {Math.min(page, totalPages)} de {totalPages}</span>
          <button type="button" className="secondary" disabled={page >= totalPages}
            onClick={() => setPage(page + 1)}>Siguiente</button>
        </div>}
      </>)}
  </>
}
