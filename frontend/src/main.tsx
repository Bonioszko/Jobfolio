import { StrictMode, useEffect, useState } from 'react'
import { createRoot } from 'react-dom/client'
import './styles.css'

type Status = { code: string; label: string }
type Domain = { sourceItem: { singular: string; plural: string }; generatedDocument: { singular: string; plural: string }; statuses: Status[] }
type Item = { id: string; sourceKey: string; displayTitle: string; parsedData: { producer?: string; price?: { amount: number; currency: string } }; workflowStatus: string; parserKey: string; parserVersion: number; sourceReceivedAt: string; demoEmailHtml?: string }
type Template = { id: string; name: string; version: number; versionId: string; tex: string }
type Rules = { id: string; version: number; versionId: string; markdown: string }
type Job = { id: string; status: string; error?: string; outputId?: string }
type Document = { id: string; version: number; versionId: string; tex: string }

async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, { credentials: 'include', headers: { 'Content-Type': 'application/json' }, ...init })
  if (!response.ok) throw new Error((await response.text()) || `${response.status}`)
  return response.status === 204 ? undefined as T : response.json()
}
const wait = (ms: number) => new Promise(resolve => setTimeout(resolve, ms))

function App() {
  const [domain, setDomain] = useState<Domain>()
  const [authenticated, setAuthenticated] = useState(false)
  const [items, setItems] = useState<Item[]>([])
  const [selected, setSelected] = useState<Item>()
  const [templates, setTemplates] = useState<Template[]>([])
  const [rules, setRules] = useState<Rules>()
  const [busy, setBusy] = useState('')
  const [document, setDocument] = useState<Document>()
  const [pdfId, setPdfId] = useState<string>()
  const [error, setError] = useState('')

  useEffect(() => { api<Domain>('/api/config/domain').then(setDomain); api('/api/me').then(() => setAuthenticated(true)).catch(() => undefined) }, [])
  useEffect(() => { if (authenticated) Promise.all([api<Item[]>('/api/source-items'), api<Template[]>('/api/templates'), api<Rules>('/api/rules')]).then(([i,t,r]) => { setItems(i); setTemplates(t); setRules(r) }).catch(showError) }, [authenticated])
  const showError = (value: unknown) => setError(value instanceof Error ? value.message : String(value))
  const startDemo = async () => { try { await api('/api/auth/demo', { method: 'POST' }); setAuthenticated(true) } catch (e) { showError(e) } }
  const changeStatus = async (item: Item, status: string) => { await api(`/api/source-items/${item.id}/status`, { method: 'PUT', body: JSON.stringify({ status }) }); setItems(xs => xs.map(x => x.id === item.id ? { ...x, workflowStatus: status } : x)) }
  const inspect = async (id: string) => setSelected(await api<Item>(`/api/source-items/${id}`))
  const generate = async () => {
    if (!selected || !templates[0] || !rules) return
    try { setBusy('Queued for generation'); const created = await api<Job>('/api/generation-jobs', { method: 'POST', body: JSON.stringify({ sourceItemId: selected.id, templateVersionId: templates[0].versionId, ruleVersionId: rules.versionId }) });
      let job = created; while (['Queued','Running'].includes(job.status)) { setBusy(job.status === 'Queued' ? 'Queued for generation' : 'Generating…'); await wait(1000); job = await api<Job>(`/api/generation-jobs/${job.id}`) }
      if (job.status !== 'Succeeded' || !job.outputId) throw new Error(job.error || 'Generation failed'); setDocument(await api<Document>(`/api/documents/${job.outputId}`)); setBusy('Generated')
    } catch (e) { showError(e); setBusy('') }
  }
  const compile = async () => {
    if (!document) return
    try { setBusy('Queued for compilation'); const created = await api<Job>('/api/compile-jobs', { method: 'POST', body: JSON.stringify({ documentVersionId: document.versionId }) });
      let job = created; while (['Queued','Running'].includes(job.status)) { setBusy(job.status === 'Queued' ? 'Queued for compilation' : 'Compiling…'); await wait(1000); job = await api<Job>(`/api/compile-jobs/${job.id}`) }
      if (job.status !== 'Succeeded' || !job.outputId) throw new Error(job.error || 'Compilation failed'); setPdfId(job.outputId); setBusy('PDF ready')
    } catch (e) { showError(e); setBusy('') }
  }

  if (!domain) return <main className="center">Loading…</main>
  if (!authenticated) return <main className="landing"><div><span className="eyebrow">EMAIL → TEX → PDF</span><h1>Turn incoming emails into polished documents.</h1><p>A secure, deterministic parsing workflow with versioned generation and isolated PDF compilation.</p><button onClick={startDemo}>Try Demo</button><small>No account required · isolated for 6 hours</small>{error && <p className="error">{error}</p>}</div></main>
  return <main><header><div><span className="eyebrow">WORKSPACE</span><h1>{domain.sourceItem.plural}</h1></div><span className="demo">Demo session</span></header>{error && <div className="error banner">{error}</div>}
    <section className="grid"><div className="card table"><table><thead><tr><th>Received</th><th>Source</th><th>Title</th><th>Producer</th><th>Price</th><th>Status</th></tr></thead><tbody>{items.map(item => <tr key={item.id} onClick={() => inspect(item.id)}><td>{new Date(item.sourceReceivedAt).toLocaleDateString()}</td><td>{item.sourceKey}</td><td>{item.displayTitle}</td><td>{item.parsedData.producer}</td><td>{item.parsedData.price?.amount.toLocaleString()} {item.parsedData.price?.currency}</td><td onClick={e => e.stopPropagation()}><select value={item.workflowStatus} onChange={e => changeStatus(item, e.target.value)}>{domain.statuses.map(s => <option key={s.code} value={s.code}>{s.label}</option>)}</select></td></tr>)}</tbody></table></div>
    <aside className="card">{selected ? <><span className="eyebrow">{selected.parserKey} · parser v{selected.parserVersion}</span><h2>{selected.displayTitle}</h2><div className="email" dangerouslySetInnerHTML={{ __html: selected.demoEmailHtml || '' }}/><label>Template<select>{templates.map(t => <option key={t.id}>{t.name} · v{t.version}</option>)}</select></label><button onClick={generate}>Generate {domain.generatedDocument.singular}</button>{busy && <p className="progress">{busy}</p>}{document && <><textarea value={document.tex} readOnly/><button onClick={compile}>Compile PDF</button></>}{pdfId && <><iframe title="PDF preview" src={`/api/pdf-artifacts/${pdfId}/download`}/><a className="download" href={`/api/pdf-artifacts/${pdfId}/download`}>Download PDF</a></>}</> : <p className="muted">Select a {domain.sourceItem.singular.toLowerCase()} to inspect the parsed source.</p>}</aside></section></main>
}
createRoot(document.getElementById('root')!).render(<StrictMode><App /></StrictMode>)
