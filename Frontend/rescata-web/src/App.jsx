import { useEffect, useSyncExternalStore } from 'react'
import { BrowserRouter, Link, Navigate, NavLink, Route, Routes, useLocation } from 'react-router-dom'
import Login from './auth/Login.jsx'
import { decodeSession, endSession, getToken, subscribeSession } from './auth/session.js'
import { areas, canAccess, roleNames } from './auth/access.js'

function Workspace() {
  const token = useSyncExternalStore(subscribeSession, getToken)
  const session = decodeSession(token)
  const expiresAt = session?.expiresAt
  const location = useLocation()

  useEffect(() => {
    if (!token) return
    if (!expiresAt) {
      endSession(token)
      return
    }
    const check = () => { if (Date.now() >= expiresAt) endSession(token) }
    const timer = window.setTimeout(check, expiresAt - Date.now())
    window.addEventListener('focus', check)
    return () => {
      window.clearTimeout(timer)
      window.removeEventListener('focus', check)
    }
  }, [token, expiresAt])

  if (!session) {
    if (location.pathname === '/login') return <Login />
    return <Navigate to="/login" state={{ from: location.pathname }} replace />
  }
  const from = location.state?.from
  if (location.pathname === '/login') {
    return <Navigate to={areas.some(area => area.path === from) ? from : '/'} replace />
  }
  return (
    <main className="workspace">
      <nav aria-label="Menú principal">
        <NavLink to="/" end>Mi cuenta</NavLink>
        {areas.filter(area => canAccess(area, session)).map(area =>
          <NavLink key={area.path} to={area.path}>{area.title}</NavLink>
        )}
        <button type="button" onClick={() => endSession()}>Cerrar sesión</button>
      </nav>
      <section className="card">
        <Routes>
          <Route path="/" element={<>
            <h1>Mi cuenta</h1>
            <dl><dt>Correo electrónico</dt><dd>{session.email}</dd>
              <dt>Roles</dt><dd>{session.roles.map(role => roleNames[role] ?? role).join(', ') || 'Sin roles asignados'}</dd>
            </dl>
          </>} />
          {areas.map(area => <Route key={area.path} path={area.path} element={
            canAccess(area, session)
              ? <><h1>{area.title}</h1><h2>Permisos de tu cuenta</h2>
                  <ul>{area.actions.map(action => <li key={action}>{action}</li>)}</ul></>
              : <><h1>Acceso restringido</h1><p>Tu cuenta no tiene permiso para entrar en esta sección.</p><Link to="/">Volver a mi cuenta</Link></>
          } />)}
          <Route path="*" element={<><h1>Página no encontrada</h1><Link to="/">Volver a mi cuenta</Link></>} />
        </Routes>
      </section>
    </main>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <div className="app">
        <header className="app-header">
          <span className="brand">Rescata<span aria-hidden="true">.</span></span>
          <span className="brand-description">Rescate de alimentos</span>
        </header>
        <Workspace />
      </div>
    </BrowserRouter>
  )
}
