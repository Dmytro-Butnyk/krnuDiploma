import { Tags } from 'lucide-react'
import { useConstructorStore } from '../../model/store'
import type { TagKind } from '../../model/types'

const tagTypeLabels: Record<TagKind, string> = {
  scalar: 'Скаляр',
  table_column: 'Табличний тег',
  reserved: 'Системний тег',
}

export function TagMarkupStep() {
  const tagTypes = useConstructorStore((state) => state.tagTypes)
  const setTagType = useConstructorStore((state) => state.setTagType)

  return (
    <div>
      <div className="mb-6 flex items-start gap-3">
        <div className="flex h-11 w-11 items-center justify-center rounded-lg bg-blue-50 text-blue-600">
          <Tags size={20} />
        </div>
        <div>
          <h3 className="text-lg font-black uppercase text-blue-700">1 КРОК: РОЗМІТКА ТЕГІВ</h3>
          <p className="mt-1 max-w-2xl text-sm leading-6 text-slate-500">
            Вкажіть, які теги є одиночними значеннями, а які будуть колонками всередині таблиць.
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
        {Object.entries(tagTypes).map(([tag, type]) => (
          <div
            key={tag}
            className="flex items-center justify-between rounded-lg border border-blue-100 bg-white p-3 shadow-sm"
          >
            <div>
              <span className={type === 'reserved' ? 'font-mono text-sm font-black text-slate-400' : 'font-mono text-sm font-black text-orange-600'}>{`{{${tag}}}`}</span>
              <p className="mt-1 text-xs text-slate-500">
                {tagTypeLabels[type]}
                {type === 'reserved' && ' - не потребує прив’язки'}
              </p>
            </div>
            <select
              value={type}
              onChange={(event) => setTagType(tag, event.target.value as TagKind)}
              disabled={type === 'reserved'}
              className="rounded-md border border-blue-200 bg-white px-3 py-2 text-sm font-semibold outline-none transition hover:bg-blue-50 focus:border-blue-500 disabled:cursor-not-allowed disabled:border-slate-200 disabled:bg-slate-100 disabled:text-slate-400"
            >
              <option value="scalar">Скаляр</option>
              <option value="table_column">Табличний тег</option>
              <option value="reserved">Системний тег</option>
            </select>
          </div>
        ))}
      </div>
    </div>
  )
}
