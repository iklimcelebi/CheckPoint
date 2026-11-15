import { useEffect, useRef, useState } from 'react'

export function useSearchState(delayMs = 300) {
  const [searchInput, setSearchInput] = useState('')
  const [q, setQ] = useState('')
  const [page, setPage] = useState(1)
  const timer = useRef<number | undefined>(undefined)

  useEffect(() => () => window.clearTimeout(timer.current), [])

  const onSearchChange = (value: string) => {
    setSearchInput(value)
    window.clearTimeout(timer.current)
    timer.current = window.setTimeout(() => {
      setQ(value.trim())
      setPage(1)
    }, delayMs)
  }

  return { searchInput, onSearchChange, q, page, setPage }
}
