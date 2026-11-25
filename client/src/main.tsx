import { createRoot } from 'react-dom/client'
import { lazy, Suspense, useEffect, useState } from 'react'
import { api, message, type Draft, type Summary, type User } from './api'
const Editor = lazy(() => import('./Editor'))
import './style.css'

function App() {
  const [users, setUsers] = useState<User[]>([])
  const [user, setUser] = useState(Number(localStorage.getItem('draftspace-user')) || 1)
  const [id, setId] = useState(location.hash.slice(1))
  const [documents, setDocuments] = useState<Summary[]>([])
  const [draft, setDraft] = useState<Draft | null>(null)
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [attempt, setAttempt] = useState(0)

  useEffect(() => {
    const sync = () => setId(location.hash.slice(1))
    window.addEventListener('hashchange', sync)
    return () => window.removeEventListener('hashchange', sync)
  }, [])

  useEffect(() => {
    const controller = new AbortController()
    const init = { signal: controller.signal }
    setLoading(true)
    setDraft(null)
    setError('')
    Promise.all([
      api<User[]>('users', undefined, init),
      id ? api<Draft>('documents/' + encodeURIComponent(id), user, init)
         : api<Summary[]>('documents', user, init),
    ]).then(([people, result]) => {
      if (controller.signal.aborted) return
      setUsers(people)
      if (Array.isArray(result)) setDocuments(result)
      else setDraft(result)
    }).catch(e => { if (!controller.signal.aborted) setError(message(e)) })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [user, id, attempt])

  async function create(file?: File) {
    setBusy(true)
    setError('')
    try {
      const form = file ? new FormData() : undefined
      if (file && (!/\.txt$/i.test(file.name) || !file.size || file.size > 262144))
        throw new Error('Choose a .txt file between 1 byte and 256 KB.')
      if (file) form?.append('file', file)
      const doc = await api<Draft>(file ? 'documents/import' : 'documents', user, { method: 'POST', body: form })
      location.hash = doc.id
    } catch (e) { setError(message(e)) }
    finally { setBusy(false) }
  }

  if (id && draft?.id === id) return <Suspense fallback={<main className="dashboard" role="status">Opening document&hellip;</main>}><Editor key={user + ':' + id} draft={draft} user={user} users={users} /></Suspense>

  return <>
    <header className="top"><div className="brand"><span aria-hidden="true">&#9636;</span> DraftSpace</div>
      <label className="switch">Demo user
        <select aria-label="Demo user" value={user} disabled={busy || users.length === 0} onChange={e => {
          const next = Number(e.target.value)
          localStorage.setItem('draftspace-user', String(next))
          setUser(next)
        }}>{users.map(person => <option key={person.id} value={person.id}>{person.name}</option>)}</select>
      </label>
    </header>
    <main className="dashboard">
      <div className="intro"><div><h1>Documents</h1><p>Write and share documents in your workspace.</p></div>
        {id ? <a className="button" href="#">Documents</a> :
          <div className="flex flex-wrap gap-2">
          <input hidden id="import-file" type="file" accept=".txt,text/plain" onChange={e => {
            const file = e.currentTarget.files?.[0]
            e.currentTarget.value = ''
            if (file) void create(file)
          }} />
          <button className="button" disabled={busy || loading || !users.length} onClick={() => document.getElementById('import-file')?.click()}>Import document</button>
          <button className="primary" disabled={busy || loading || !users.length} onClick={() => void create()}>+ New document</button>
        </div>}
      </div>
      {error && <div className="error" role="alert">{error} <button onClick={() => setAttempt(n => n + 1)}>Retry</button></div>}
      {loading ? <p role="status">Loading documents&hellip;</p> : !id && users.length > 0 && <>
        <DocumentList heading="My Documents" documents={documents.filter(doc => doc.isOwner)} empty="Create or import a document to get started." />
        <DocumentList heading="Shared with Me" documents={documents.filter(doc => !doc.isOwner)} empty="Documents shared with this demo user will appear here." />
      </>}
    </main>
  </>
}

function DocumentList({ heading, documents, empty }: { heading: string; documents: Summary[]; empty: string }) {
  return <section className="collection"><h2>{heading}</h2>
    {documents.length ? <ul>{documents.map(doc => <li key={doc.id}>
      <a className="document" href={'#' + doc.id}>
        <span className="file-icon" aria-hidden="true">&#9636;</span>
        <span className="details"><strong>{doc.title}</strong><small>{doc.isOwner ? 'Owned by you' : 'Owned by ' + doc.owner.name}</small></span>
        <time dateTime={doc.updatedAt}>{new Date(doc.updatedAt).toLocaleDateString(undefined, { dateStyle: 'medium' })}</time>
      </a>
    </li>)}</ul> : <p className="empty">{empty}</p>}
  </section>
}

createRoot(document.getElementById('root')!).render(<App />)


