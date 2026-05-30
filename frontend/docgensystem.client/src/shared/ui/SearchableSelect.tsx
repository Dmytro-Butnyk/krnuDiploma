import { ChevronDown } from 'lucide-react'
import { useMemo, useState } from 'react'
import { cn } from '../lib/cn'

type SearchableSelectOption = {
  value: string
  label: string
}

type SearchableSelectProps = {
  value: string
  options: SearchableSelectOption[]
  placeholder: string
  emptyText?: string
  className?: string
  maxVisibleOptions?: number
  getOptionSearchScore?: (option: SearchableSelectOption, query: string) => number
  onChange: (value: string) => void
}

export function SearchableSelect({
  value,
  options,
  placeholder,
  emptyText = 'Нічого не знайдено',
  className,
  maxVisibleOptions,
  getOptionSearchScore,
  onChange,
}: SearchableSelectProps) {
  const [query, setQuery] = useState('')
  const [isOpen, setIsOpen] = useState(false)
  const selectedLabel = options.find((option) => option.value === value)?.label ?? ''
  const visibleOptions = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase()
    const rankedOptions = normalizedQuery && getOptionSearchScore
      ? options
          .map((option, index) => ({
            option,
            index,
            score: getOptionSearchScore(option, normalizedQuery),
          }))
          .filter((item) => Number.isFinite(item.score))
          .sort((left, right) => {
            if (right.score !== left.score) return right.score - left.score
            return left.index - right.index
          })
          .map((item) => item.option)
      : normalizedQuery
        ? options.filter((option) => option.label.toLowerCase().includes(normalizedQuery))
        : options

    return typeof maxVisibleOptions === 'number' ? rankedOptions.slice(0, maxVisibleOptions) : rankedOptions
  }, [getOptionSearchScore, maxVisibleOptions, options, query])

  return (
    <div className={cn('relative', className)}>
      <div className="ui-input flex min-h-[50px] items-center">
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
          className="min-h-[48px] w-full rounded-[var(--radius-ui-sm)] bg-transparent px-4 py-2 text-base font-medium text-[var(--color-text)] outline-none placeholder:text-[var(--color-muted)]"
        />
        <ChevronDown size={18} className="mr-4 shrink-0 text-[var(--color-muted)]" />
      </div>

      {isOpen && (
        <div className="custom-scrollbar absolute left-0 right-0 top-[calc(100%+8px)] z-30 max-h-64 overflow-auto rounded-[var(--radius-ui-sm)] border border-[var(--color-primary)] bg-white p-2 shadow-[var(--shadow-ui-strong)]">
          {visibleOptions.map((option) => (
            <button
              key={option.value}
              type="button"
              className={cn(
                'block w-full rounded-[14px] px-4 py-3 text-left text-base font-bold transition',
                option.value === value
                  ? 'bg-[var(--color-primary-hover)] text-white hover:bg-[var(--color-primary-hover)] active:bg-[var(--color-primary)]'
                  : 'text-[var(--color-text)] hover:bg-[var(--color-bg-lavender)] active:bg-[var(--color-primary)] active:text-white',
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
          {visibleOptions.length === 0 && <div className="px-4 py-3 text-sm font-semibold text-slate-400">{emptyText}</div>}
        </div>
      )}
    </div>
  )
}
