import { ChevronDown } from 'lucide-react'
import { useMemo, useState } from 'react'
import { cn } from '../lib/cn'

type SearchableSelectProps = {
  value: string
  options: Array<{ value: string; label: string }>
  placeholder: string
  emptyText?: string
  className?: string
  onChange: (value: string) => void
}

export function SearchableSelect({
  value,
  options,
  placeholder,
  emptyText = 'Нічого не знайдено',
  className,
  onChange,
}: SearchableSelectProps) {
  const [query, setQuery] = useState('')
  const [isOpen, setIsOpen] = useState(false)
  const selectedLabel = options.find((option) => option.value === value)?.label ?? ''
  const visibleOptions = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase()
    if (!normalizedQuery) return options

    return options.filter((option) => option.label.toLowerCase().includes(normalizedQuery))
  }, [options, query])

  return (
    <div className={cn('relative', className)}>
      <div className="flex items-center rounded-md border border-blue-200 bg-white focus-within:border-blue-500 focus-within:ring-2 focus-within:ring-blue-100">
        <input
          value={isOpen ? query : selectedLabel}
          onChange={(event) => {
            setQuery(event.target.value)
            setIsOpen(true)
          }}
          onFocus={() => {
            setQuery('')
            setIsOpen(true)
          }}
          onBlur={() => window.setTimeout(() => setIsOpen(false), 120)}
          placeholder={placeholder}
          className="min-h-10 w-full rounded-md bg-transparent px-3 py-2 text-sm outline-none"
        />
        <ChevronDown size={16} className="mr-3 shrink-0 text-slate-400" />
      </div>

      {isOpen && (
        <div className="absolute left-0 right-0 top-[calc(100%+6px)] z-30 max-h-56 overflow-auto rounded-lg border border-blue-200 bg-white p-1 shadow-xl">
          {visibleOptions.map((option) => (
            <button
              key={option.value}
              type="button"
              className={cn(
                'block w-full rounded-md px-3 py-2 text-left text-sm transition',
                option.value === value
                  ? 'bg-blue-600 font-bold text-white hover:bg-blue-600 active:bg-blue-700'
                  : 'hover:bg-blue-50 active:bg-blue-100',
              )}
              onMouseDown={(event) => {
                event.preventDefault()
                onChange(option.value)
                setQuery('')
                setIsOpen(false)
              }}
            >
              {option.label}
            </button>
          ))}
          {visibleOptions.length === 0 && <div className="px-3 py-2 text-sm text-slate-400">{emptyText}</div>}
        </div>
      )}
    </div>
  )
}
