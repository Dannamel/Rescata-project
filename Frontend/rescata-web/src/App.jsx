export default function App() {
  return (
    <div className="app">
      <header className="app-header">
        <span className="brand">Rescata<span aria-hidden="true">.</span></span>
        <span className="brand-description">Rescate de alimentos</span>
      </header>

      <main className="welcome" aria-labelledby="welcome-title">
        <p className="eyebrow">Conectamos para compartir</p>
        <h1 id="welcome-title">Cada alimento cuenta.</h1>
        <p className="welcome-description">
          Unimos a negocios y fundaciones para que los excedentes de comida
          lleguen a quienes los necesitan.
        </p>
      </main>

      <footer className="app-footer">
        Menos desperdicio. Más oportunidades para compartir.
      </footer>
    </div>
  )
}
