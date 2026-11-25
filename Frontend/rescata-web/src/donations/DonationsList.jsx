import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { listDonations, listFoodCategories } from './api.js'
import { filterableStatuses, formatDate, formatQuantity, readFilters, statusNames, writeFilters } from './format.js'
import StatusBadge from './StatusBadge.jsx'

export default function DonationsList() {
  const [params, setParams] = useSearchParams()
  const { page, status, categoryId } = readFilters(params)
  const [categories, setCategories] = useState({ items: [], error: '' })
  const [attempt, setAttempt] = useState(0)
  // Cada resultado guarda la clave de la consulta que lo produjo. Mientras la clave no coincide con la
  // consulta actual, la respuesta está en camino: se muestran los datos anteriores (para que la vista
  // no salte), pero no se usan para decidir nada sobre la consulta actual.
  const queryKey = [page, status, categoryId, attempt].join('|')
  const [result, setResult] = useState({ key: null, data: null, error: '' })
  const isCurrent = result.key === queryKey
  const loading = !isCurrent

  useEffect(() => {
    const controller = new AbortController()
    listFoodCategories(controller.signal)
      .then(items => setCategories({ items: Array.isArray(items) ? items : [], error: '' }))
      .catch(error => {
        if (error.name !== 'AbortError') setCategories({ items: [], error: 'No se pudieron cargar las categorías.' })
      })
    return () => controller.abort()
  }, [])

  useEffect(() => {
    const controller = new AbortController()
    const key = [page, status, categoryId, attempt].join('|')
    listDonations({ page, status, categoryId }, controller.signal)
      .then(data => setResult({ key, data, error: '' }))
      .catch(error => {
        if (error.name !== 'AbortError') setResult({ key, data: null, error: error.message })
      })
    return () => controller.abort()
  }, [page, status, categoryId, attempt])

  const data = result.data
  const error = isCurrent ? result.error : ''
  const totalPages = data?.totalPages ?? 0
  // Solo el totalPages de la respuesta a la consulta actual sirve para validar la página.
  const currentTotalPages = isCurrent ? totalPages : 0

  // Si la página pedida quedó fuera de rango (por ejemplo, al editar la URL), se va a la última.
  useEffect(() => {
    if (currentTotalPages > 0 && page > currentTotalPages) {
      setParams(writeFilters({ page: currentTotalPages, status, categoryId }), { replace: true })
    }
  }, [page, currentTotalPages, status, categoryId, setParams])

  function changeFilter(change) {
    setParams(writeFilters({ status, categoryId, ...change, page: 1 }))
  }

  function goToPage(next) {
    setParams(writeFilters({ status, categoryId, page: next }))
  }

  const search = params.toString() ? '?' + params : ''

  return <>
    <h1>Donaciones</h1>
    <div className="filters">
      <div>
        <label htmlFor="filter-status">Estado</label>
        <select id="filter-status" value={status} onChange={event => changeFilter({ status: event.target.value })}>
          <option value="">Todos los estados</option>
          {filterableStatuses.map(value => <option key={value} value={value}>{statusNames[value]}</option>)}
        </select>
      </div>
      <div>
        <label htmlFor="filter-category">Categoría</label>
        <select id="filter-category" value={categoryId} disabled={!categories.items.length && !categoryId}
          onChange={event => changeFilter({ categoryId: event.target.value })}>
          <option value="">Todas las categorías</option>
          {categories.items.map(category => <option key={category.id} value={category.id}>{category.name}</option>)}
          {categoryId && !categories.items.some(category => category.id === categoryId) &&
            <option value={categoryId}>Categoría seleccionada</option>}
        </select>
        {categories.error && <p className="error">{categories.error}</p>}
      </div>
      {(status || categoryId) &&
        <button type="button" className="secondary" onClick={() => setParams(new URLSearchParams())}>Quitar filtros</button>}
    </div>

    <p role="status" className="loading">{loading ? 'Cargando donaciones…' : ''}</p>
    {error && <div role="alert">
      <p className="error">{error}</p>
      <button type="button" onClick={() => setAttempt(value => value + 1)}>Reintentar</button>
    </div>}
    {data && <div aria-busy={loading} className={loading ? 'is-loading' : undefined}>{data.items.length === 0
      ? <p>{status || categoryId ? 'No hay donaciones que coincidan con los filtros.' : 'Todavía no hay donaciones publicadas.'}</p>
      : <>
        <p className="results-count">{data.totalCount === 1 ? '1 donación' : data.totalCount + ' donaciones'}</p>
        <ul className="donation-list">
          {data.items.map(donation => <li key={donation.id}>
            <div className="donation-heading">
              <Link to={'/donaciones/' + donation.id} state={{ search }}>{donation.title}</Link>
              <StatusBadge status={donation.status} />
            </div>
            <p className="donation-meta">
              <span>{donation.foodCategory || 'Sin categoría'}</span>
              <span>{formatQuantity(donation.quantityAmount, donation.quantityUnit)}</span>
              <span>Disponible hasta {formatDate(donation.availableUntil)}</span>
            </p>
          </li>)}
        </ul>
        {totalPages > 1 && <div className="pagination" role="navigation" aria-label="Paginación">
          <button type="button" className="secondary" disabled={page <= 1} onClick={() => goToPage(page - 1)}>Anterior</button>
          <span aria-current="page">Página {Math.min(page, totalPages)} de {totalPages}</span>
          <button type="button" className="secondary" disabled={page >= totalPages} onClick={() => goToPage(page + 1)}>Siguiente</button>
        </div>}
      </>}</div>}
  </>
}
