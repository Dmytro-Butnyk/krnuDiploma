import { Check, Loader2, RotateCcw, Sparkles, Trash2 } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import type { EntitySchema } from '../../../../entities/schema/model/types'
import { cn } from '../../../../shared/lib/cn'
import { Button } from '../../../../shared/ui/Button'
import { useConstructorScenarios } from '../../api/scenarioApi'
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
  const dataSetupMode = useConstructorStore((state) => state.dataSetupMode)
  const appliedScenarioId = useConstructorStore((state) => state.appliedScenarioId)
  const config = useConstructorStore((state) => state.config)
  const setDataSetupMode = useConstructorStore((state) => state.setDataSetupMode)
  const applyScenario = useConstructorStore((state) => state.applyScenario)
  const cancelScenario = useConstructorStore((state) => state.cancelScenario)
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
  const scenariosQuery = useConstructorScenarios()
  const [scenarioError, setScenarioError] = useState<string | null>(null)

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

  const handleApplyScenario = (scenarioId: string) => {
    const scenario = scenariosQuery.data?.find((item) => item.id === scenarioId)
    if (!scenario) return

    const result = applyScenario(scenario)
    setScenarioError(result.ok ? null : result.reason)
  }

  const handleCancelScenario = () => {
    cancelScenario()
    setScenarioError(null)
  }

  return (
    <div className="h-full min-h-0 overflow-y-auto overflow-x-hidden pr-1 custom-scrollbar">
      <h3 className="ui-step-title">2 КРОК: ДЖЕРЕЛА ДАНИХ</h3>
      <p className="ui-lead mt-5 max-w-3xl">
        Оберіть сутність, за якою шаблон отримуватиме дані. Поле для форми генерації буде створено автоматично.
      </p>

      <div className="mt-6 flex flex-wrap gap-2 rounded-[var(--radius-ui-sm)] border border-[var(--color-bg-lavender)] bg-white p-2 shadow-[var(--shadow-ui)]">
        <button
          type="button"
          onClick={() => {
            setDataSetupMode('manual')
            setScenarioError(null)
          }}
          className={cn(
            'rounded-[var(--radius-ui-sm)] px-4 py-2 text-sm font-extrabold transition',
            dataSetupMode === 'manual'
              ? 'bg-[var(--color-primary)] text-white'
              : 'text-[var(--color-primary)] hover:bg-[var(--color-bg-lavender)]',
          )}
        >
          Ручне налаштування
        </button>
        <button
          type="button"
          onClick={() => {
            setDataSetupMode('scenario')
            setScenarioError(null)
          }}
          className={cn(
            'inline-flex items-center gap-2 rounded-[var(--radius-ui-sm)] px-4 py-2 text-sm font-extrabold transition',
            dataSetupMode === 'scenario'
              ? 'bg-[var(--color-primary)] text-white'
              : 'text-[var(--color-primary)] hover:bg-[var(--color-bg-lavender)]',
          )}
        >
          <Sparkles size={15} />
          Сценарій
        </button>
      </div>

      {dataSetupMode === 'scenario' && (
        <div className="mt-5 rounded-[var(--radius-ui-md)] border border-[var(--color-bg-lavender)] bg-[var(--color-surface)] p-5 shadow-[var(--shadow-ui)]">
          <div className="mb-4">
            <h4 className="text-lg font-extrabold text-[var(--color-primary)]">Попередньо налаштовані сценарії</h4>
            <p className="mt-2 text-sm font-bold text-[var(--color-muted)]">
              Сценарій додає тільки Inputs і DataSources. Маппінг тегів залишається гнучким.
            </p>
          </div>

          {scenariosQuery.isLoading && (
            <div className="flex items-center text-sm font-bold text-[var(--color-primary)]">
              <Loader2 className="mr-2 animate-spin" size={18} />
              Завантаження сценаріїв
            </div>
          )}

          {scenariosQuery.isError && (
            <div className="rounded-[var(--radius-ui-sm)] border border-[var(--color-danger)] bg-[var(--color-danger-soft)] p-4 text-sm font-bold text-[var(--color-danger)]">
              Не вдалося отримати сценарії з `/api/constructor/scenarios`.
            </div>
          )}

          {scenarioError && (
            <div className="mb-4 rounded-[var(--radius-ui-sm)] border border-[var(--color-danger)] bg-[var(--color-danger-soft)] p-4 text-sm font-bold text-[var(--color-danger)]">
              {scenarioError}
            </div>
          )}

          {appliedScenarioId && (
            <div className="mb-4 flex flex-wrap items-center justify-between gap-3 rounded-[var(--radius-ui-sm)] border border-[var(--color-bg-lavender)] bg-white p-4 shadow-[var(--shadow-ui)]">
              <div>
                <p className="text-sm font-extrabold text-[var(--color-primary)]">Сценарій застосовано</p>
                <p className="mt-1 text-xs font-bold text-[var(--color-muted)]">
                  Обов'язкові теги та джерела сценарію налаштовані автоматично.
                </p>
              </div>
              <Button variant="secondary" size="sm" onClick={handleCancelScenario}>
                <RotateCcw size={14} />
                Скасувати сценарій
              </Button>
            </div>
          )}

          <div className="grid grid-cols-1 gap-3 lg:grid-cols-2">
            {scenariosQuery.data?.map((scenario) => {
              const isApplied = scenario.id === appliedScenarioId

              return (
                <article key={scenario.id} className="rounded-[var(--radius-ui-sm)] border border-[var(--color-bg-lavender)] bg-white p-4 shadow-[var(--shadow-ui)]">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <h5 className="text-base font-extrabold text-[var(--color-primary)]">{scenario.title}</h5>
                      <p className="mt-2 text-sm font-semibold leading-5 text-[var(--color-muted)]">{scenario.description}</p>
                    </div>
                    {isApplied && <Check className="shrink-0 text-[var(--color-success)]" size={20} />}
                  </div>

                  <div className="mt-4 flex flex-wrap gap-2 text-[11px] font-bold uppercase text-[var(--color-muted)]">
                    <span className="rounded-[10px] bg-[var(--color-bg-lavender)] px-2 py-1">
                      Inputs: {Object.keys(scenario.inputs).join(', ')}
                    </span>
                    <span className="rounded-[10px] bg-[var(--color-bg-lavender)] px-2 py-1">
                      DataSources: {scenario.dataSources.map((source) => source.Key).join(', ')}
                    </span>
                  </div>

                  {scenario.recommendedTableSources.length > 0 && (
                    <div className="mt-3 text-xs font-bold text-[var(--color-muted)]">
                      Рекомендовано для таблиць: {scenario.recommendedTableSources.map((source) => source.key).join(', ')}
                    </div>
                  )}

                  <Button
                    variant={isApplied ? 'secondary' : 'primary'}
                    size="sm"
                    className="mt-4"
                    onClick={() => handleApplyScenario(scenario.id)}
                    disabled={isApplied}
                  >
                    {isApplied ? 'Застосовано' : 'Застосувати'}
                  </Button>
                </article>
              )
            })}
          </div>
        </div>
      )}

      {dataSetupMode === 'manual' && (
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
      )}

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
