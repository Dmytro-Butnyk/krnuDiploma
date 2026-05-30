import { Check, Trash2 } from 'lucide-react'
import { useEffect, useMemo } from 'react'
import type { EntitySchema } from '../../../../entities/schema/model/types'
import { cn } from '../../../../shared/lib/cn'
import { Button } from '../../../../shared/ui/Button'
import { useConstructorStore } from '../../model/store'
import type { DataSourceFilterOperator } from '../../model/types'

type Props = {
  schema?: EntitySchema
}

function isIntField(schema: EntitySchema, entity: string, field: string) {
  const node = schema[entity]
  if (!node) return field === 'Id' || field.endsWith('Id')

  return (
    node.keyScalars.includes(field) ||
    node.foreignKeys.some((foreignKey) => foreignKey.property === field) ||
    field === 'Id' ||
    field.endsWith('Id')
  )
}

function getOperatorOptions(isInt: boolean): Array<{ value: DataSourceFilterOperator; label: string }> {
  return isInt
    ? [
        { value: 'Equals', label: 'Дорівнює' },
        { value: 'NotEquals', label: 'Не дорівнює' },
      ]
    : [
        { value: 'Equals', label: 'Дорівнює' },
        { value: 'Contains', label: 'Містить' },
      ]
}

function getFilterPreview(field: string, operator: DataSourceFilterOperator) {
  if (operator === 'NotEquals') return `${field} != @0`
  if (operator === 'Contains') return `${field} != null && ${field}.Contains(@0)`
  return `${field} == @0`
}

function getInputKey(entity: string, field: string) {
  return `${entity}${field}`
}

export function DataSourcesStep({ schema = {} }: Props) {
  const newSource = useConstructorStore((state) => state.newSource)
  const config = useConstructorStore((state) => state.config)
  const updateNewSource = useConstructorStore((state) => state.updateNewSource)
  const toggleParentFilterProperty = useConstructorStore((state) => state.toggleParentFilterProperty)
  const validateNewDataSource = useConstructorStore((state) => state.validateNewDataSource)
  const addDataSource = useConstructorStore((state) => state.addDataSource)
  const removeDataSource = useConstructorStore((state) => state.removeDataSource)
  const entityNames = useMemo(() => Object.keys(schema), [schema])
  const entityNode = schema[newSource.entity]
  const scalarFields = entityNode?.scalars ?? []
  const selectedFieldIsInt = isIntField(schema, newSource.entity, newSource.filterProperty)
  const filterOperator = newSource.filterOperator ?? 'Equals'
  const operatorOptions = getOperatorOptions(selectedFieldIsInt)
  const validationResult = validateNewDataSource()
  const validationReason = validationResult.ok ? null : validationResult.reason
  const generatedInputKey = getInputKey(newSource.entity, newSource.filterProperty || 'Id')
  const parentSuggestions = entityNode?.foreignKeys ?? []
  const selectedParentFilterProperties = (newSource.parentFilterProperties ?? []).slice(0, 1)

  useEffect(() => {
    if (!newSource.entity && entityNames[0]) {
      const entity = entityNames[0]
      updateNewSource({
        entity,
        key: `Target${entity}`,
        filterProperty: schema[entity]?.keyScalars[0] ?? schema[entity]?.scalars[0] ?? 'Id',
        filterOperator: 'Equals',
        argumentLabel: getInputKey(entity, schema[entity]?.keyScalars[0] ?? schema[entity]?.scalars[0] ?? 'Id'),
        parentFilterProperties: [],
      })
    }
  }, [entityNames, newSource.entity, schema, updateNewSource])

  useEffect(() => {
    if (operatorOptions.some((option) => option.value === filterOperator)) return
    updateNewSource({ filterOperator: 'Equals' })
  }, [filterOperator, operatorOptions, updateNewSource])

  const handleEntityChange = (entity: string) => {
    const filterProperty = schema[entity]?.keyScalars[0] ?? schema[entity]?.scalars[0] ?? 'Id'
    updateNewSource({
      entity,
      key: `Target${entity}`,
      filterProperty,
      filterOperator: 'Equals',
      argumentLabel: getInputKey(entity, filterProperty),
      parentFilterProperties: [],
    })
  }

  const handleFieldChange = (filterProperty: string) => {
    updateNewSource({
      filterProperty,
      filterOperator: 'Equals',
      argumentLabel: getInputKey(newSource.entity, filterProperty),
      parentFilterProperties: [],
    })
  }

  const handleAdd = () => {
    const result = addDataSource(schema)
    if (!result.ok) window.alert(result.reason)
  }

  return (
    <div className="h-full min-h-0 overflow-y-auto overflow-x-hidden pr-1 custom-scrollbar">
      <h3 className="ui-step-title">2 КРОК: ДЖЕРЕЛА ДАНИХ</h3>
      <p className="ui-lead mt-5 max-w-3xl">
        Оберіть сутність, за якою шаблон отримуватиме дані. Поле для форми генерації буде створено автоматично.
      </p>

      <div className="mt-8 rounded-[var(--radius-ui-md)] border border-[var(--color-bg-lavender)] bg-[var(--color-surface)] p-5 shadow-[var(--shadow-ui)]">
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          <label className="block">
            <span className="ui-label mb-2 block">Сутність бази даних</span>
            <select
              value={newSource.entity}
              onChange={(event) => handleEntityChange(event.target.value)}
              className="ui-input w-full px-4 py-3 text-sm font-bold"
            >
              {entityNames.map((entity) => (
                <option key={entity} value={entity}>
                  {entity}
                </option>
              ))}
            </select>
          </label>

          <label className="block">
            <span className="ui-label mb-2 block">Ключ датасурсу</span>
            <input
              value={newSource.key}
              onChange={(event) => updateNewSource({ key: event.target.value })}
              className="ui-input w-full px-4 py-3 text-sm font-bold"
              required
              placeholder="TargetStudent"
            />
          </label>
        </div>

        <div className="mt-5 grid grid-cols-1 gap-4 border-t border-[var(--color-bg-lavender)] pt-5 md:grid-cols-3">
          <label className="block">
            <span className="ui-label mb-2 block">Поле фільтра</span>
            <select
              value={newSource.filterProperty}
              onChange={(event) => handleFieldChange(event.target.value)}
              className="ui-input w-full px-4 py-3 text-sm font-bold"
            >
              {scalarFields.map((scalar) => (
                <option key={scalar} value={scalar}>
                  {scalar}
                </option>
              ))}
            </select>
          </label>

          <label className="block">
            <span className="ui-label mb-2 block">Оператор</span>
            <select
              value={filterOperator}
              onChange={(event) => updateNewSource({ filterOperator: event.target.value as DataSourceFilterOperator })}
              className="ui-input w-full px-4 py-3 text-sm font-bold"
            >
              {operatorOptions.map((operator) => (
                <option key={operator.value} value={operator.value}>
                  {operator.label}
                </option>
              ))}
            </select>
          </label>

          <label className="block">
            <span className="ui-label mb-2 block">Назва аргументу</span>
            <input
              value={newSource.argumentLabel}
              onChange={(event) => updateNewSource({ argumentLabel: event.target.value })}
              className="ui-input w-full px-4 py-3 text-sm font-bold"
              placeholder="Група"
            />
          </label>
        </div>

        {selectedFieldIsInt && parentSuggestions.length > 0 && (
          <div className="mt-5 rounded-[var(--radius-ui-sm)] border border-[var(--color-bg-lavender)] bg-white p-4">
            <p className="ui-label mb-3">Додаткове обмеження вибору</p>
            <div className="flex flex-wrap gap-2">
              {parentSuggestions.map((suggestion) => {
                const isSelected = selectedParentFilterProperties.includes(suggestion.property)

                return (
                  <button
                    key={suggestion.property}
                    type="button"
                    onClick={() => toggleParentFilterProperty(suggestion.property)}
                    className={cn(
                      'inline-flex items-center gap-2 rounded-[var(--radius-ui-pill)] border px-4 py-2 text-xs font-bold transition',
                      isSelected
                        ? 'border-[var(--color-primary)] bg-[var(--color-primary)] text-white'
                        : 'border-[var(--color-primary)] bg-white text-[var(--color-primary)] hover:bg-[var(--color-bg-lavender)]',
                    )}
                  >
                    {isSelected && <Check size={13} />}
                    {suggestion.targetEntity} через {suggestion.property}
                  </button>
                )
              })}
            </div>
            <p className="mt-3 text-xs font-bold text-[var(--color-muted)]">
              Якщо обрати залежність, у формі генерації користувач спочатку вибере батьківську сутність, а потім відфільтрований список.
            </p>
          </div>
        )}

        <div className="mt-5 flex flex-wrap items-start justify-between gap-3">
          <div className="overflow-x-auto rounded-[12px] bg-[var(--color-bg-lavender)] p-3 font-mono text-xs text-[var(--color-muted)]">
            Фільтр: {getFilterPreview(newSource.filterProperty || 'Id', filterOperator)} | Аргументи: [{generatedInputKey}]
          </div>
          <div className="flex flex-col items-end gap-2">
            <Button variant="primary" size="pill" className="h-[58px] min-h-[58px] text-base" onClick={handleAdd} disabled={Boolean(validationReason)}>
              Додати датасурс
            </Button>
            {validationReason && (
              <span className="max-w-md text-right text-xs font-bold text-[var(--color-danger)]">
                {validationReason}
              </span>
            )}
          </div>
        </div>
      </div>

      <ul className="mt-5 space-y-3">
        {config.DataSources.map((source) => (
          <li key={source.Key} className="rounded-[var(--radius-ui-sm)] border border-[var(--color-bg-lavender)] bg-white p-4 shadow-[var(--shadow-ui)]">
            <div className="flex items-center justify-between gap-3">
              <div>
                <span className="font-extrabold text-[var(--color-primary)]">{source.Key}</span>
                <span className="ml-2 text-xs font-bold text-[var(--color-muted)]">({source.Entity})</span>
              </div>
              <Button variant="danger" size="sm" onClick={() => removeDataSource(source.Key)}>
                <Trash2 size={14} />
                Видалити
              </Button>
            </div>
            <div className="mt-3 overflow-x-auto rounded-[12px] bg-[var(--color-bg-lavender)] p-3 font-mono text-xs text-[var(--color-muted)]">
              Фільтр: {source.Filter ?? 'null'} | Аргументи: {JSON.stringify(source.FilterArgs)}
            </div>
          </li>
        ))}
      </ul>
    </div>
  )
}
