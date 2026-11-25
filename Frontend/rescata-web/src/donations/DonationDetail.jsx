import { useEffect, useState } from 'react'
import { Link, useLocation, useParams } from 'react-router-dom'
import { getDonation } from './api.js'
import StatusBadge from './StatusBadge.jsx'
import { formatDate, formatQuantity } from './format.js'

export default function DonationDetail() {
  const { id } = useParams()
  const location = useLocation()
  const [result, setResult] = useState({ state: 'loading' })
  const [attempt, setAttempt] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    setResult({ state: 'loading' })
    getDonation(id, controller.signal)
      .then(donation => setResult({ state: 'ready', donation }))
      .catch(error => {
        if (error.name !== 'AbortError') setResult({ state: 'error', message: error.message })
      })
    return () => controller.abort()
  }, [id, attempt])

  // Vuelve al listado con los mismos filtros y página desde los que se abrió el detalle.
  const search = location.state?.search
  const back = <Link to={'/donaciones' + (typeof search === 'string' && search.startsWith('?') ? search : '')}>Volver al listado</Link>

  if (result.state === 'loading') return <><p role="status">Cargando donación…</p>{back}</>
  if (result.state === 'error') return <>
    <h1>No se pudo cargar la donación</h1>
    <p className="error" role="alert">{result.message} Es posible que la donación no exista.</p>
    <div className="detail-actions">
      <button type="button" onClick={() => setAttempt(value => value + 1)}>Reintentar</button>
      {back}
    </div>
  </>

  const { donation } = result
  return <>
    {back}
    <div className="donation-heading detail-heading">
      <h1>{donation.title}</h1>
      <StatusBadge status={donation.status} />
    </div>
    <dl className="donation-detail">
      <dt>Descripción</dt><dd>{donation.description || 'Sin descripción'}</dd>
      <dt>Categoría</dt><dd>{donation.foodCategory || 'Sin categoría'}</dd>
      <dt>Cantidad</dt><dd>{formatQuantity(donation.quantityAmount, donation.quantityUnit)}</dd>
      <dt>Dirección de recogida</dt><dd>{donation.pickupAddress}</dd>
      <dt>Disponible hasta</dt><dd>{formatDate(donation.availableUntil)}</dd>
      <dt>Publicada</dt><dd>{formatDate(donation.createdAt)}</dd>
    </dl>
  </>
}
