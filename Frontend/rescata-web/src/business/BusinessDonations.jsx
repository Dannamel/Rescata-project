import { useCallback, useEffect, useState, useSyncExternalStore } from 'react'
import { Link } from 'react-router-dom'
import { cancelDonation, listDonations, listFoodCategories } from '../donations/api.js'
import { formatDate, formatQuantity } from '../donations/format.js'
import StatusBadge from '../donations/StatusBadge.jsx'
import { decodeSession, getToken, subscribeSession } from '../auth/session.js'
import DonationForm from './DonationForm.jsx'
import { canCancel } from './options.js'

export default function BusinessDonations() {
  const session = decodeSession(useSyncExternalStore(subscribeSession, getToken))
  const businessId = session?.userId ?? ''
  const [page, setPage] = useState(1)
  const [attempt, setAttempt] = useState(0)
  const [categories, setCategories] = useState({ items: [], error: '' })
  const [result, setResult] = useState({ state: 'loading' })
  const [cancelling, setCancelling] = useState('')
  const [cancelError, setCancelError] = useState('')

  useEffect(() => {
    const controller = new AbortController()
    listFoodCategories(controller.signal)
      .then(items => setCategories({ items: Array.isArray(items) ? items : [], error: '' }))
      .catch(error => {
        if (error.name !== 'AbortError') {
          setCategories({ items: [], error: 'No se pudieron cargar las categorías.' })
        }
      })
    return () => controller.abort()
  }, [])

  useEffect(() => {
    if (!businessId) return
    const controller = new AbortController()
    setResult({ state: 'loading' })
    listDonations({ page, businessId }, controller.signal)
      .then(data => setResult({ state: 'ready', data }))
      .catch(error => {
        if (error.name !== 'AbortError') setResult({ state: 'error', message: error.message })
      })
    return () => controller.abort()
  }, [businessId, page, attempt])

  const reload = useCallback(() => {
    setCancelError('')
    setPage(1)
    setAttempt(value => value + 1)
  }, [])

  async function cancel(id) {
    if (cancelling) return
    setCancelling(id)
    setCancelError('')
    try {
      await cancelDonation(id)
      setAttempt(value => value + 1)
    } catch (error) {
      setCancelError(error.message)
    } finally {
      setCancelling('')
    }
  }

  const data = result.state === 'ready' ? result.data : null
  const totalPages = data?.totalPages ?? 0

  return <>
    <h1>Negocio</h1>

    <DonationForm
      businessId={businessId}
      categories={categories.items}
      categoriesError={categories.error}
      onCreated={reload} />

    <h2>Mis donaciones</h2>

    <p role="status" className="loading">{result.state === 'loading' ? 'Cargando tus donaciones…' : ''}</p>

    {cancelError && <p className="error" role="alert">{cancelError}</p>}

    {result.state === 'error' && <div role="alert">
      <p className="error">{result.message}</p>
      <button type="button" onClick={() => setAttempt(value => value + 1)}>Reintentar</button>
    </div>}

    {data && (data.items.length === 0
      ? <p>Todavía no has publicado donaciones.</p>
      : <>
        <p className="results-count">{data.totalCount === 1 ? '1 donación' : data.totalCount + ' donaciones'}</p>
        <ul className="donation-list">
          {data.items.map(donation => <li key={donation.id}>
            <div className="donation-heading">
              <Link to={'/donaciones/' + donation.id}>{donation.title}</Link>
              <StatusBadge status={donation.status} />
            </div>
            <p className="donation-meta">
              <span>{donation.foodCategory || 'Sin categoría'}</span>
              <span>{formatQuantity(donation.quantityAmount, donation.quantityUnit)}</span>
              <span>Disponible hasta {formatDate(donation.availableUntil)}</span>
            </p>
            {canCancel(donation.status) && <div className="donation-actions">
              <button type="button" className="secondary" disabled={cancelling === donation.id}
                onClick={() => cancel(donation.id)}>
                {cancelling === donation.id ? 'Cancelando…' : 'Cancelar donación'}
              </button>
            </div>}
          </li>)}
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
