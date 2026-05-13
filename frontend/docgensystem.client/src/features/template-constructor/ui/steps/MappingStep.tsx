import { ChevronDown, ChevronRight, Plus, Trash2, X } from 'lucide-react'
import { useMemo } from 'react'
import type { EntitySchema, SchemaPath } from '../../../../entities/schema/model/types'
import { cn } from '../../../../shared/lib/cn'
import { getPathsForEntity, getSourceArrayScalarPaths } from '../../../../shared/lib/paths'
import { Button } from '../../../../shared/ui/Button'
import { SearchableSelect } from '../../../../shared/ui/SearchableSelect'
import { useConstructorStore } from '../../model/store'

type Props = {
  schema?: EntitySchema
}

function ScalarPathList({
  sourceKey,
  entity,
  paths,
}: {
  sourceKey: string
  entity: string
  paths: SchemaPath[]
}) {
  const expandedSources = useConstructorStore((state) => state.expandedSources)
  const toggleExpanded = useConstructorStore((state) => state.toggleExpanded)
  const selectedTag = useConstructorStore((state) => state.selectedTag)
  const mapScalar = useConstructorStore((state) => state.mapScalar)
  const isExpanded = expandedSources[sourceKey] ?? true

  return (
    <div className="overflow-hidden rounded-lg border border-blue-100 bg-white shadow-sm">
      <button
        className="flex w-full items-center justify-between bg-blue-50 px-3 py-2 text-left text-sm font-black text-blue-800 transition hover:bg-blue-100 active:bg-blue-200"
        onClick={() => toggleExpanded(sourceKey)}
      >
        <span>
          {sourceKey}
          <span className="ml-2 text-xs font-semibold text-slate-500">({entity})</span>
        </span>
        {isExpanded ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
      </button>
      {isExpanded && (
        <ul className="max-h-72 overflow-auto p-2 custom-scrollbar">
          {paths.map((path) => (
            <li key={path.fullPath}>
              <button
                disabled={!selectedTag}
                onClick={() => selectedTag && mapScalar(selectedTag, `${sourceKey}.${path.fullPath}`)}
                className="group flex w-full items-center justify-between rounded-md px-2 py-1.5 text-left font-mono text-sm text-slate-700 transition hover:bg-blue-50 active:bg-blue-100 disabled:cursor-not-allowed disabled:opacity-60"
              >
                <span>{path.fullPath}</span>
                <span className="text-xs font-black text-blue-600 opacity-0 transition group-hover:opacity-100">
                  Зв'язати
                </span>
              </button>
            </li>
          ))}
          {paths.length === 0 && <li className="p-3 text-sm text-slate-400">Нічого не знайдено</li>}
        </ul>
      )}
    </div>
  )
}

export function MappingStep({ schema = {} }: Props) {
  const mappingMode = useConstructorStore((state) => state.mappingMode)
  const setMappingMode = useConstructorStore((state) => state.setMappingMode)
  const selectedTag = useConstructorStore((state) => state.selectedTag)
  const setSelectedTag = useConstructorStore((state) => state.setSelectedTag)
  const selectedTable = useConstructorStore((state) => state.selectedTable)
  const setSelectedTable = useConstructorStore((state) => state.setSelectedTable)
  const newTableName = useConstructorStore((state) => state.newTableName)
  const setNewTableName = useConstructorStore((state) => state.setNewTableName)
  const newColumnTag = useConstructorStore((state) => state.newColumnTag)
  const setNewColumnTag = useConstructorStore((state) => state.setNewColumnTag)
  const newColumnPath = useConstructorStore((state) => state.newColumnPath)
  const setNewColumnPath = useConstructorStore((state) => state.setNewColumnPath)
  const searchQuery = useConstructorStore((state) => state.searchQuery)
  const setSearchQuery = useConstructorStore((state) => state.setSearchQuery)
  const tagTypes = useConstructorStore((state) => state.tagTypes)
  const config = useConstructorStore((state) => state.config)
  const unmapScalar = useConstructorStore((state) => state.unmapScalar)
  const createNewTable = useConstructorStore((state) => state.createNewTable)
  const deleteTable = useConstructorStore((state) => state.deleteTable)
  const renameTable = useConstructorStore((state) => state.renameTable)
  const updateTableSourceArray = useConstructorStore((state) => state.updateTableSourceArray)
  const addColumnToTable = useConstructorStore((state) => state.addColumnToTable)
  const removeColumnFromTable = useConstructorStore((state) => state.removeColumnFromTable)

  const scalarTags = useMemo(
    () => Object.keys(tagTypes).filter((tag) => tagTypes[tag] === 'scalar'),
    [tagTypes],
  )
  const tableColumnTags = useMemo(
    () => Object.keys(tagTypes).filter((tag) => tagTypes[tag] === 'table_column'),
    [tagTypes],
  )
  const activeTable = selectedTable ? config.Mapping.Tables[selectedTable] : null
  const usedTableTags = activeTable ? Object.keys(activeTable.RowMapping) : []
  const availableTableTags = tableColumnTags.filter((tag) => !usedTableTags.includes(tag))
  const sourceArrayPaths = activeTable
    ? getSourceArrayScalarPaths(schema, config.DataSources, activeTable.SourceArray)
    : []
  const sourceArrayOptions = useMemo(
    () =>
      config.DataSources.flatMap((source) => [
        { value: source.Key, label: `${source.Key} (${source.Entity})` },
        ...getPathsForEntity(schema, source.Entity)
          .filter((path) => path.isCollection)
          .map((path) => ({
            value: `${source.Key}.${path.fullPath}`,
            label: `${source.Key}.${path.fullPath}`,
          })),
      ]),
    [config.DataSources, schema],
  )
  const sourcePathOptions = sourceArrayPaths.map((path) => ({ value: path, label: path }))

  const getFilteredScalarPaths = (entity: string) => {
    const allPaths = getPathsForEntity(schema, entity).filter((path) => !path.isCollection)
    if (!searchQuery) return allPaths
    const query = searchQuery.toLowerCase()
    return allPaths.filter((path) => path.fullPath.toLowerCase().includes(query))
  }

  return (
    <div className="flex min-h-[548px] flex-col">
      <div className="mb-4 flex items-start justify-between gap-4">
        <div>
          <h3 className="text-lg font-black uppercase text-blue-700">3 КРОК: МАППІНГ</h3>
          <p className="mt-1 max-w-2xl text-sm font-semibold leading-6 text-slate-600">
            Оберіть, який тип тегів потрібно налаштувати.
          </p>
        </div>
        <div className="flex rounded-full border border-blue-100 bg-white p-1 shadow-sm">
          <button
            className={cn(
              'rounded-full px-5 py-2 text-sm font-black transition hover:bg-blue-50 active:bg-blue-100',
              mappingMode === 'scalars' ? 'border border-orange-500 text-orange-600' : 'text-slate-500',
            )}
            onClick={() => setMappingMode('scalars')}
          >
            Скаляри
          </button>
          <button
            className={cn(
              'rounded-full px-5 py-2 text-sm font-black transition hover:bg-blue-50 active:bg-blue-100',
              mappingMode === 'tables' ? 'border border-orange-500 text-orange-600' : 'text-slate-500',
            )}
            onClick={() => setMappingMode('tables')}
          >
            Таблиці
          </button>
        </div>
      </div>

      {mappingMode === 'scalars' ? (
        <div className="grid flex-1 grid-cols-1 gap-4 lg:grid-cols-[260px_minmax(0,1fr)]">
          <div className="rounded-xl border border-blue-100 bg-white p-3 shadow-sm">
            <p className="mb-3 text-xs font-black uppercase tracking-wide text-slate-500">Оберіть тег</p>
            {scalarTags.map((tag) => {
              const isMapped = Boolean(config.Mapping.Scalars[tag])
              return (
                <button
                  key={tag}
                  onClick={() => setSelectedTag(tag)}
                  className={cn(
                    'relative mb-2 w-full rounded-lg border p-3 text-left text-sm font-black transition',
                    selectedTag === tag || isMapped
                      ? 'border-blue-700 bg-blue-600 text-white hover:bg-blue-600 active:bg-blue-700'
                      : 'border-blue-200 bg-white text-blue-700 hover:bg-blue-50 active:bg-blue-100',
                  )}
                >
                  {tag}
                  {isMapped && (
                    <>
                      <span className="mt-1 block truncate pr-6 text-[11px] font-semibold text-blue-100">
                        {config.Mapping.Scalars[tag]}
                      </span>
                      <span
                        role="button"
                        tabIndex={0}
                        onClick={(event) => {
                          event.stopPropagation()
                          unmapScalar(tag)
                        }}
                        className="absolute right-2 top-1/2 flex h-6 w-6 -translate-y-1/2 items-center justify-center rounded text-red-100 hover:bg-white hover:text-red-500"
                      >
                        <X size={14} />
                      </span>
                    </>
                  )}
                </button>
              )
            })}
          </div>

          <div className="overflow-hidden rounded-xl border border-blue-100 bg-white shadow-sm">
            <div className="border-b border-blue-50 bg-white p-3">
              <input
                value={searchQuery}
                onChange={(event) => setSearchQuery(event.target.value)}
                className="w-full rounded-lg border border-blue-200 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                placeholder="Пошук властивостей"
              />
            </div>
            <div className="grid max-h-[430px] gap-3 overflow-auto p-3 custom-scrollbar">
              {!selectedTag && <div className="p-10 text-center text-sm text-slate-400">Оберіть тег ліворуч</div>}
              {selectedTag &&
                config.DataSources.map((source) => (
                  <ScalarPathList
                    key={source.Key}
                    sourceKey={source.Key}
                    entity={source.Entity}
                    paths={getFilteredScalarPaths(source.Entity)}
                  />
                ))}
            </div>
          </div>
        </div>
      ) : (
        <div className="grid flex-1 grid-cols-1 gap-3 lg:grid-cols-[220px_minmax(0,1fr)]">
          <div className="rounded-xl border border-blue-100 bg-white p-3 shadow-sm">
            <Button variant="success" className="mb-5 w-full rounded-full text-xs" onClick={createNewTable}>
              <Plus size={16} />
              Створити таблицю
            </Button>
            <p className="mb-3 text-xs font-black uppercase tracking-wide text-slate-500">Оберіть таблицю</p>
            {Object.entries(config.Mapping.Tables).map(([tableName, table]) => (
              <button
                key={tableName}
                onClick={() => setSelectedTable(tableName)}
                className={cn(
                  'mb-2 w-full rounded-lg border p-3 text-left text-sm font-black transition',
                  selectedTable === tableName
                    ? 'border-blue-700 bg-blue-600 text-white hover:bg-blue-600 active:bg-blue-700'
                    : 'border-blue-200 bg-white text-blue-700 hover:bg-blue-50 active:bg-blue-100',
                )}
              >
                {tableName}
                <span className="mt-1 block truncate text-[11px] font-semibold opacity-70">
                  {table.SourceArray || 'Джерело не вибрано'}
                </span>
              </button>
            ))}
          </div>

          <div className="rounded-xl border border-blue-100 bg-white p-4 shadow-sm">
            {!selectedTable || !activeTable ? (
              <div className="p-10 text-center text-sm text-slate-400">Створіть або оберіть таблицю</div>
            ) : (
              <>
                <div className="mb-4 flex items-start justify-between gap-4">
                  <div>
                    <h4 className="text-xl font-black text-blue-700">{selectedTable}</h4>
                    <label className="mt-3 block max-w-sm">
                      <span className="mb-1 block text-xs font-bold uppercase text-slate-500">
                        Системна назва таблиці
                      </span>
                      <input
                        value={newTableName}
                        onChange={(event) => setNewTableName(event.target.value)}
                        onBlur={() => renameTable(selectedTable, newTableName)}
                        className="w-full rounded-lg border border-blue-200 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500"
                      />
                    </label>
                  </div>
                  <Button variant="danger" className="min-h-8 rounded-full px-4 py-1 text-xs" onClick={() => deleteTable(selectedTable)}>
                    <Trash2 size={16} />
                    Видалити
                  </Button>
                </div>

                <div className="mb-5">
                  <span className="mb-1 block text-xs font-black uppercase tracking-wide text-slate-500">
                    Джерело колекції
                  </span>
                  <SearchableSelect
                    value={activeTable.SourceArray}
                    options={sourceArrayOptions}
                    placeholder="Виберіть колекцію бази даних"
                    onChange={(value) => updateTableSourceArray(selectedTable, value)}
                  />
                </div>

                {activeTable.SourceArray && (
                  <>
                    <p className="mb-2 text-xs font-black uppercase tracking-wide text-slate-500">Прив'язка тегів</p>
                    <div className="mb-4 grid grid-cols-1 gap-2 rounded-xl border border-blue-100 bg-blue-50/40 p-3 md:grid-cols-[minmax(130px,0.8fr)_minmax(180px,1.2fr)_auto]">
                      <select
                        value={newColumnTag}
                        onChange={(event) => setNewColumnTag(event.target.value)}
                        className="min-w-0 rounded-lg border border-blue-200 bg-white px-3 py-2 font-mono text-sm font-bold text-blue-700 outline-none focus:border-blue-500"
                      >
                        <option value="">Тег</option>
                        {availableTableTags.map((tag) => (
                          <option key={tag} value={tag}>
                            {tag}
                          </option>
                        ))}
                      </select>
                      <SearchableSelect
                        value={newColumnPath}
                        options={sourcePathOptions}
                        placeholder="Поле"
                        onChange={setNewColumnPath}
                      />
                      <Button
                        className="min-h-10 rounded-lg px-4"
                        onClick={addColumnToTable}
                        disabled={!newColumnTag || !newColumnPath}
                      >
                        OK
                      </Button>
                    </div>

                    <ul className="space-y-2">
                      {Object.entries(activeTable.RowMapping).map(([tag, path]) => (
                        <li
                          key={tag}
                          className="flex items-center justify-between rounded-lg border border-blue-100 bg-white p-3 text-sm shadow-sm"
                        >
                          <span className="min-w-0">
                            <span className="rounded bg-blue-50 px-2 py-1 font-mono font-black text-blue-700">
                              {tag}
                            </span>
                            <span className="mx-2 text-slate-400">→</span>
                            <span className="font-mono text-slate-600">{path}</span>
                          </span>
                          <button
                            className="flex h-8 w-8 items-center justify-center rounded text-red-500 hover:bg-red-50"
                            onClick={() => removeColumnFromTable(selectedTable, tag)}
                            title="Видалити колонку"
                          >
                            <X size={16} />
                          </button>
                        </li>
                      ))}
                    </ul>
                  </>
                )}
              </>
            )}
          </div>
        </div>
      )}
    </div>
  )
}
