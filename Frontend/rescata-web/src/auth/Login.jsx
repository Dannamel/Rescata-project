import { useState } from 'react'
import { apiRequest } from '../api/client.js'
import { startSession } from './session.js'

export default function Login() {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  async function submit(event) {
    event.preventDefault()
    if (busy) return
    const fields = new FormData(event.currentTarget)
    setBusy(true)
    setError('')
    try {
      const result = await apiRequest('/identity/api/auth/login', {
        method: 'POST',
        authenticated: false,
        body: { email: fields.get('email').trim(), password: fields.get('password') },
      })
      startSession(result.data?.accessToken)
    } catch (err) {
      setError(err.message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <main className="login-layout">
      <section className="welcome">
        <h1>Cada alimento cuenta.</h1>
        <p className="welcome-description">Conectamos negocios y fundaciones para que los excedentes de comida lleguen a quienes los necesitan.</p>
      </section>
      <form className="card" onSubmit={submit}>
        <h2>Iniciar sesión</h2>
        <label htmlFor="email">Correo electrónico</label>
        <input id="email" name="email" type="email" autoComplete="username" required />
        <label htmlFor="password">Contraseña</label>
        <input id="password" name="password" type="password" autoComplete="current-password" required />
        {error && <p className="error" role="alert">{error}</p>}
        <button disabled={busy}>{busy ? 'Ingresando…' : 'Ingresar'}</button>
      </form>
    </main>
  )
}
