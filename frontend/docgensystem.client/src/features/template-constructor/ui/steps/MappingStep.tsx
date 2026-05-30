import { ChevronDown, ChevronRight, Plus, Trash2, X } from 'lucide-react'
import { useMemo, useState } from 'react'
import type { EntitySchema, SchemaPath } from '../../../../entities/schema/model/types'
import { cn } from '../../../../shared/lib/cn'
import { getPathsForEntity, getSourceArrayScalarPaths } from '../../../../shared/lib/paths'
import { Button } from '../../../../shared/ui/Button'
import { SearchableSelect } from '../../../../shared/ui/SearchableSelect'
import { getTableRowTagName, getTableTagPrefix, useConstructorStore } from '../../model/store'

type Props = {
  schema?: EntitySchema
}

const MAX_VISIBLE_MAPPING_PATHS = 200

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
  const visiblePaths = paths.slice(0, MAX_VISIBLE_MAPPING_PATHS)

  return (
    <div className="overflow-hidden rounded-[var(--radius-ui-sm)] border border-[var(--color-bg-lavender)] bg-white shadow-[var(--shadow-ui)]">
      <button
        className="flex w-full items-center justify-between bg-[var(--color-bg-lavender)] px-4 py-3 text-left text-sm font-extrabold text-[var(--color-primary)] transition hover:bg-[var(--color-primary-hover)] hover:text-white active:bg-[var(--color-primary)]"
        onClick={() => toggleExpanded(sourceKey)}
      >
        <span>
          {sourceKey}
          <span className="ml-2 text-xs font-bold text-[var(--color-muted)]">({entity})</span>
        </span>
        {isExpanded ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
      </button>
      {isExpanded && (
        <ul className="p-2">
          {visiblePaths.map((path) => (
            <li key={path.fullPath}>
              <button
                disabled={!selectedTag}
                onClick={() => selectedTag && mapScalar(selectedTag, `${sourceKey}.${path.fullPath}`)}
                className="group flex w-full items-center justify-between rounded-[12px] px-3 py-2 text-left font-mono text-sm text-[var(--color-text)] transition hover:bg-[var(--color-bg-lavender)] active:bg-[var(--color-primary)] active:text-white disabled:cursor-not-allowed disabled:opacity-60"
              >
                <span>{path.fullPath}</span>
                <span className="text-xs font-extrabold text-[var(--color-primary)] opacity-0 transition group-hover:opacity-100 group-active:text-white">
                  Звʼязати
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

function InputScalarPanel({ tag }: { tag: string }) {
  const config = useConstructorStore((state) => state.config)
  const mapInputScalar = useConstructorStore((state) => state.mapInputScalar)
  const existingInputKey = config.Mapping.Scalars[tag]?.startsWith('Input.')
    ? config.Mapping.Scalars[tag].slice('Input.'.length)
    : null
  const existingLabel = existingInputKey ? config.Inputs[existingInputKey]?.Label : null
  const [label, setLabel] = useState(existingLabel ?? tag)

  return (
    <div className="rounded-[var(--radius-ui-md)] border border-[var(--color-bg-lavender)] bg-white p-5 shadow-[var(--shadow-ui)]">
      <h4 className="text-lg font-extrabold text-[var(--color-primary)]">{tag}</h4>
      <p className="mt-2 text-sm font-bold text-[var(--color-muted)]">
        Це поле користувач заповнить вручну перед генерацією. Значення буде збережено як текст.
      </p>
      <label className="mt-5 block">
        <span className="ui-label mb-2 block">Назва поля у формі генерації</span>
        <input
          value={label}
          onChange={(event) => setLabel(event.target.value)}
          className="ui-input w-full px-4 py-3 text-sm font-bold"
          placeholder="Дата підписання"
        />
      </label>
      <Button className="mt-4" onClick={() => mapInputScalar(tag, label)} disabled={!label.trim()}>
        Застосувати
      </Button>
    </div>
  )
}

function getPathSearchScore(path: string, query: string) {
  const normalizedPath = path.toLowerCase()
  const normalizedQuery = query.trim().toLowerCase()
  if (!normalizedQuery) return 0
  if (!normalizedPath.includes(normalizedQuery)) return Number.NEGATIVE_INFINITY

  const segments = normalizedPath.split('.')
  const bestSegmentScore = segments.reduce((bestScore, segment, index) => {
    const matchIndex = segment.indexOf(normalizedQuery)
    if (matchIndex === -1) return bestScore

    let score = 500
    if (matchIndex === 0) score += 700
    if (segment === normalizedQuery) score += 300
    if (index === 0) score += 260
    if (index === segments.length - 1) score += 40
    score -= matchIndex * 25

    return Math.max(bestScore, score)
  }, 0)

  const fullPathMatchIndex = normalizedPath.indexOf(normalizedQuery)
  let fullPathScore = 400
  if (fullPathMatchIndex === 0) fullPathScore += 400
  if (normalizedPath === normalizedQuery) fullPathScore += 300
  fullPathScore -= fullPathMatchIndex * 10
  const depthPenalty = (segments.length - 1) * 140
  const lengthPenalty = normalizedPath.length * 0.5

  return Math.max(bestSegmentScore, fullPathScore) - depthPenalty - lengthPenalty
}

function getPathCost(path: string) {
  const segments = path.split('.')
  return (segments.length - 1) * 100 + path.length * 0.5
}

function rankSchemaPaths(paths: SchemaPath[], query: string) {
  const normalizedQuery = query.trim()
  if (!normalizedQuery) {
    return [...paths].sort((left, right) => {
      const costDifference = getPathCost(left.fullPath) - getPathCost(right.fullPath)
      return costDifference || left.fullPath.localeCompare(right.fullPath)
    })
  }

  return paths
    .map((path, index) => ({
      path,
      index,
      score: getPathSearchScore(path.fullPath, normalizedQuery),
    }))
    .filter((item) => Number.isFinite(item.score))
    .sort((left, right) => {
      if (right.score !== left.score) return right.score - left.score
      const lengthDifference = left.path.fullPath.length - right.path.fullPath.length
      return lengthDifference || left.index - right.index
    })
    .map((item) => item.path)
}

function rankPathValues(paths: string[], query: string) {
  return rankSchemaPaths(
    paths.map((fullPath) => ({ fullPath, isCollection: false as const })),
    query,
  ).map((path) => path.fullPath)
}

function getOptionPathSearchScore(option: { label: string }, query: string) {
  return getPathSearchScore(option.label, query)
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
    () => Object.keys(tagTypes).filter((tag) => tagTypes[tag] === 'db_scalar' || tagTypes[tag] === 'input_scalar'),
    [tagTypes],
  )
  const selectedTagKind = selectedTag ? tagTypes[selectedTag] : null
  const tableColumnTags = useMemo(
    () => Object.keys(tagTypes).filter((tag) => tagTypes[tag] === 'table_column'),
    [tagTypes],
  )
  const activeTable = selectedTable ? config.Mapping.Tables[selectedTable] : null
  const scopedTableTags = useMemo(() => {
    if (!selectedTable) return tableColumnTags

    const matchingPrefixTags = tableColumnTags.filter((tag) => getTableTagPrefix(tag) === selectedTable)
    return matchingPrefixTags.length > 0 ? matchingPrefixTags : tableColumnTags
  }, [selectedTable, tableColumnTags])
  const usedTableTags = activeTable ? new Set(Object.keys(activeTable.RowMapping)) : new Set<string>()
  const availableTableTags = scopedTableTags.filter((tag) => !usedTableTags.has(getTableRowTagName(tag)))
  const sourceArrayPaths = activeTable
    ? getSourceArrayScalarPaths(schema, config.DataSources, activeTable.SourceArray)
    : []
  const sourceArrayOptions = useMemo(
    () =>
      config.DataSources.flatMap((source) => [
        { value: source.Key, label: `${source.Key} (${source.Entity})` },
        ...rankSchemaPaths(getPathsForEntity(schema, source.Entity).filter((path) => path.isCollection), '')
          .map((path) => ({
            value: `${source.Key}.${path.fullPath}`,
            label: `${source.Key}.${path.fullPath}`,
          })),
      ]),
    [config.DataSources, schema],
  )
  const sourcePathOptions = rankPathValues(sourceArrayPaths, '').map((path) => ({ value: path, label: path }))

  const getFilteredScalarPaths = (entity: string) => {
    const allPaths = getPathsForEntity(schema, entity).filter((path) => !path.isCollection)
    return rankSchemaPaths(allPaths, searchQuery)
  }

  return (
    <div className="flex min-h-0 flex-col overflow-visible pr-1 lg:pr-0">
      <div className="mb-5 flex shrink-0 flex-wrap items-start justify-between gap-4">
        <div>
          <h3 className="ui-step-title">3 КРОК: МАППІНГ</h3>
          <p className="ui-lead mt-5 max-w-3xl">Оберіть, який тип тегів потрібно налаштувати</p>
        </div>
        <div className="flex rounded-full border border-[var(--color-bg-lavender)] bg-white p-1 shadow-[var(--shadow-ui)]">
          <button
            className={cn(
              'rounded-full px-5 py-2 text-sm font-extrabold transition hover:bg-[var(--color-bg-lavender)] active:bg-[var(--color-accent)] active:text-white',
              mappingMode === 'scalars' ? 'border border-[var(--color-accent)] text-[var(--color-accent)]' : 'text-[var(--color-muted)]',
            )}
            onClick={() => setMappingMode('scalars')}
          >
            Скаляри
          </button>
          <button
            className={cn(
              'rounded-full px-5 py-2 text-sm font-extrabold transition hover:bg-[var(--color-bg-lavender)] active:bg-[var(--color-accent)] active:text-white',
              mappingMode === 'tables' ? 'border border-[var(--color-accent)] text-[var(--color-accent)]' : 'text-[var(--color-muted)]',
            )}
            onClick={() => setMappingMode('tables')}
          >
            Таблиці
          </button>
        </div>
      </div>

      {mappingMode === 'scalars' ? (
        <div className="grid grid-cols-1 gap-4 lg:grid-cols-[clamp(260px,22%,340px)_minmax(0,1fr)]">
          <div className="rounded-[var(--radius-ui-md)] border border-[var(--color-bg-lavender)] bg-white p-4 shadow-[var(--shadow-ui)]">
            <p className="ui-label mb-3">Оберіть тег</p>
            {scalarTags.map((tag) => {
              const isMapped = Boolean(config.Mapping.Scalars[tag])
              return (
                <button
                  key={tag}
                  onClick={() => setSelectedTag(tag)}
                  className={cn(
                    'relative mb-2 w-full rounded-[var(--radius-ui-sm)] border p-3 text-left text-sm font-extrabold transition',
                    selectedTag === tag || isMapped
                      ? 'border-[var(--color-primary)] bg-[var(--color-primary)] text-white hover:bg-[var(--color-primary-hover)] active:bg-[var(--color-primary)]'
                      : 'border-[var(--color-primary)] bg-white text-[var(--color-primary)] hover:bg-[var(--color-bg-lavender)] active:bg-[var(--color-primary)] active:text-white',
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
                        className="absolute right-2 top-1/2 flex h-6 w-6 -translate-y-1/2 items-center justify-center rounded text-red-100 hover:bg-white hover:text-[var(--color-danger)]"
                      >
                        <X size={14} />
                      </span>
                    </>
                  )}
                </button>
              )
            })}
          </div>

          <div className="flex min-h-[240px] flex-col rounded-[var(--radius-ui-md)] border border-[var(--color-bg-lavender)] bg-white shadow-[var(--shadow-ui)]">
            <div className="shrink-0 border-b border-[var(--color-bg-lavender)] bg-white p-3">
              <input
                value={searchQuery}
                onChange={(event) => setSearchQuery(event.target.value)}
                className="ui-input w-full px-4 py-3 text-base font-medium"
                placeholder="Пошук властивостей"
              />
            </div>
            <div className="grid content-start gap-3 p-3">
              {!selectedTag && <div className="p-10 text-center text-sm text-slate-400">Оберіть тег ліворуч</div>}
              {selectedTag && selectedTagKind === 'input_scalar' && <InputScalarPanel key={selectedTag} tag={selectedTag} />}
              {selectedTag && selectedTagKind === 'db_scalar' &&
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
        <div className="grid grid-cols-1 gap-3 lg:grid-cols-[clamp(220px,19%,300px)_minmax(0,1fr)]">
          <div className="rounded-[var(--radius-ui-md)] border border-[var(--color-bg-lavender)] bg-white p-4 shadow-[var(--shadow-ui)]">
            <Button variant="primary" size="pill" className="mb-5 w-full text-base" onClick={createNewTable}>
              <Plus size={16} />
              Створити таблицю
            </Button>
            <p className="ui-label mb-3">Оберіть таблицю</p>
            {Object.entries(config.Mapping.Tables).map(([tableName, table]) => (
              <button
                key={tableName}
                onClick={() => setSelectedTable(tableName)}
                className={cn(
                  'mb-2 w-full rounded-[var(--radius-ui-sm)] border p-3 text-left text-sm font-extrabold transition',
                  selectedTable === tableName
                    ? 'border-[var(--color-primary)] bg-[var(--color-primary)] text-white hover:bg-[var(--color-primary-hover)] active:bg-[var(--color-primary)]'
                    : 'border-[var(--color-primary)] bg-white text-[var(--color-primary)] hover:bg-[var(--color-bg-lavender)] active:bg-[var(--color-primary)] active:text-white',
                )}
              >
                {tableName}
                <span className="mt-1 block truncate text-[11px] font-semibold opacity-70">
                  {table.SourceArray || 'Джерело не вибрано'}
                </span>
              </button>
            ))}
          </div>

          <div className="min-h-[260px] rounded-[var(--radius-ui-md)] border border-[var(--color-bg-lavender)] bg-white p-5 shadow-[var(--shadow-ui)]">
            {!selectedTable || !activeTable ? (
              <div className="p-10 text-center text-sm text-slate-400">Створіть або оберіть таблицю</div>
            ) : (
              <>
                <div className="mb-4 flex items-start justify-between gap-4">
                  <div>
                    <h4 className="text-2xl font-extrabold text-[var(--color-primary)]">{selectedTable}</h4>
                    <label className="mt-3 block max-w-sm">
                      <span className="ui-label mb-2 block">Системна назва таблиці</span>
                      <input
                        value={newTableName}
                        onChange={(event) => setNewTableName(event.target.value)}
                        onBlur={() => renameTable(selectedTable, newTableName)}
                        className="ui-input w-full px-4 py-3 text-sm font-bold"
                      />
                    </label>
                  </div>
                  <Button variant="danger" size="sm" onClick={() => deleteTable(selectedTable)}>
                    <Trash2 size={16} />
                    Видалити
                  </Button>
                </div>

                <div className="mb-5">
                  <span className="ui-label mb-2 block">Джерело колекції</span>
                  <SearchableSelect
                    value={activeTable.SourceArray}
                    options={sourceArrayOptions}
                    maxVisibleOptions={MAX_VISIBLE_MAPPING_PATHS}
                    getOptionSearchScore={getOptionPathSearchScore}
                    placeholder="Виберіть колекцію бази даних"
                    onChange={(value) => updateTableSourceArray(selectedTable, value)}
                  />
                </div>

                {activeTable.SourceArray && (
                  <>
                    <p className="ui-label mb-2">Привʼязка тегів</p>
                    <div className="mb-4 grid grid-cols-1 gap-2 rounded-[var(--radius-ui-md)] border border-[var(--color-bg-lavender)] bg-[var(--color-bg-lavender)]/50 p-3 md:grid-cols-[minmax(130px,0.8fr)_minmax(180px,1.2fr)_auto]">
                      <select
                        value={newColumnTag}
                        onChange={(event) => setNewColumnTag(event.target.value)}
                        className="ui-input min-w-0 px-4 py-3 font-mono text-sm font-bold text-[var(--color-primary)]"
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
                        maxVisibleOptions={MAX_VISIBLE_MAPPING_PATHS}
                        getOptionSearchScore={getOptionPathSearchScore}
                        placeholder="Поле"
                        onChange={setNewColumnPath}
                      />
                      <Button onClick={addColumnToTable} disabled={!newColumnTag || !newColumnPath}>
                        OK
                      </Button>
                    </div>

                    <ul className="space-y-2">
                      {Object.entries(activeTable.RowMapping).map(([tag, path]) => (
                        <li
                          key={tag}
                          className="flex items-center justify-between rounded-[var(--radius-ui-sm)] border border-[var(--color-bg-lavender)] bg-white p-3 text-sm shadow-[var(--shadow-ui)]"
                        >
                          <span className="min-w-0">
                            <span className="rounded-[10px] bg-[var(--color-bg-lavender)] px-2 py-1 font-mono font-extrabold text-[var(--color-primary)]">
                              {tag}
                            </span>
                            <span className="mx-2 text-slate-400">→</span>
                            <span className="font-mono text-[var(--color-muted)]">{path}</span>
                          </span>
                          <button
                            className="flex h-8 w-8 items-center justify-center rounded text-[var(--color-danger)] transition hover:bg-[var(--color-danger-soft)]"
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
