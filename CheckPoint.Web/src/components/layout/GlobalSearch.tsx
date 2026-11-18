import { useState, type FormEvent } from 'react'
import { useLocation, useNavigate, useSearchParams } from 'react-router'

export function GlobalSearch() {
  const navigate = useNavigate()
  const location = useLocation()
  const [searchParams] = useSearchParams()
  const urlQuery = location.pathname === '/arama' ? (searchParams.get('q') ?? '') : ''
  const [value, setValue] = useState(urlQuery)
  const [syncedQuery, setSyncedQuery] = useState(urlQuery)
  if (urlQuery !== syncedQuery) {
    setSyncedQuery(urlQuery)
    setValue(urlQuery)
  }

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const q = value.trim()
    if (q) void navigate(`/arama?q=${encodeURIComponent(q)}`)
  }

  return (
    <form role="search" onSubmit={onSubmit} className="relative w-full max-w-2xl">
      <label htmlFor="global-search" className="sr-only">
        Ders, konu, kaynak veya üniversite ara
      </label>
      <svg
        viewBox="0 0 20 20"
        aria-hidden="true"
        className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400"
      >
        <circle cx="9" cy="9" r="6" fill="none" stroke="currentColor" strokeWidth="2" />
        <path d="M13.5 13.5L18 18" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
      </svg>
      <input
        id="global-search"
        type="search"
        maxLength={100}
        value={value}
        onChange={(event) => setValue(event.target.value)}
        placeholder="Ders, konu, kaynak veya üniversite ara..."
        className="block w-full rounded-full border border-peach bg-white py-2 pl-9 pr-4 text-sm text-slate-900 shadow-sm placeholder:text-slate-500 focus:border-tangerine focus:outline-2 focus:outline-offset-0 focus:outline-accent"
      />
    </form>
  )
}
