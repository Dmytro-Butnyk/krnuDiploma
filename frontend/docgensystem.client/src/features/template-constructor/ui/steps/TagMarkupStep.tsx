import { Tags } from 'lucide-react'
import { useConstructorStore } from '../../model/store'
import type { TagKind } from '../../model/types'

const tagTypeLabels: Record<TagKind, string> = {
  db_scalar: 'Скаляр бази даних',
  input_scalar: 'Скаляр введення',
  table_column: 'Табличний тег',
  reserved: 'Системний тег',
}

export function TagMarkupStep() {
  const tagTypes = useConstructorStore((state) => state.tagTypes)
  const setTagType = useConstructorStore((state) => state.setTagType)

  return (
    <div className="h-full min-h-0 overflow-auto pr-1 custom-scrollbar">
      <div className="mb-6 flex items-start gap-4">
        <div className="flex h-12 w-12 items-center justify-center rounded-[var(--radius-ui-sm)] bg-[var(--color-bg-lavender)] text-[var(--color-primary)]">
          <Tags size={20} />
        </div>
        <div>
          <h3 className="ui-step-title">1 КРОК: РОЗМІТКА ТЕГІВ</h3>
          <p className="ui-lead mt-5 max-w-4xl">
            Вкажіть, які теги є одиночними значеннями, а які будуть колонками всередині таблиць.
          </p>
        </div>
      </div>

      <div className="grid w-full grid-cols-1 gap-3 md:grid-cols-2">
        {Object.entries(tagTypes).map(([tag, type]) => (
          <div
            key={tag}
            className="grid min-w-0 grid-cols-1 gap-3 rounded-[var(--radius-ui-sm)] border border-[var(--color-bg-lavender)] bg-white p-4 shadow-[var(--shadow-ui)] sm:grid-cols-[minmax(0,1fr)_minmax(150px,200px)] sm:items-center"
          >
            <div className="min-w-0">
              <span
                className={
                  type === 'reserved'
                    ? 'block break-words text-[16px] font-semibold leading-snug text-slate-400'
                    : 'block break-words text-[16px] font-semibold leading-snug text-[var(--color-accent)]'
                }
              >
                {`{{${tag}}}`}
              </span>
              <p className="mt-2 text-sm font-bold text-[var(--color-muted)]">
                {tagTypeLabels[type]}
                {type === 'reserved' && ' - не потребує привʼязки'}
              </p>
            </div>
            <select
              value={type}
              onChange={(event) => setTagType(tag, event.target.value as TagKind)}
              disabled={type === 'reserved'}
              className="ui-input w-full min-w-0 px-4 py-3 text-sm font-bold disabled:cursor-not-allowed disabled:border-slate-200 disabled:bg-slate-100 disabled:text-slate-400"
            >
              {type === 'reserved' ? (
                  <option value="reserved">Системний тег</option>
                ) : (
                  <>
                  <option value="db_scalar">Скаляр бази даних</option>
                  <option value="input_scalar">Скаляр введення</option>
                  <option value="table_column">Табличний тег</option>
                </>
              )}
            </select>
          </div>
        ))}
      </div>
    </div>
  )
}
