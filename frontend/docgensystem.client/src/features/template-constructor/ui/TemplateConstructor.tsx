import { ArrowLeft, Loader2 } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { useConstructorSchema } from '../../../entities/schema/api/schemaApi'
import { cn } from '../../../shared/lib/cn'
import { Button } from '../../../shared/ui/Button'
import { getTableRowTagName, useConstructorStore, validateTemplateConfiguration } from '../model/store'
import type { TemplateConfiguration } from '../model/types'
import { DataSourcesStep } from './steps/DataSourcesStep'
import { MappingStep } from './steps/MappingStep'
import { ReviewStep } from './steps/ReviewStep'
import { TagMarkupStep } from './steps/TagMarkupStep'

type TemplateConstructorProps = {
  documentName: string
  templateName: string
  tags: string[]
  initialConfiguration?: TemplateConfiguration
  sessionKey: string
  isSaving?: boolean
  isTemplateFileMissing?: boolean
  canBackFromFirstStep?: boolean
  onTemplateNameChange: (name: string) => void
  onTemplateFileChange?: (file: File) => void
  onCancel: () => void
  onBack: () => void
  onComplete: (configuration: TemplateConfiguration) => void
}

const stepLabels = ['1', '2', '3', '4'] as const

export function TemplateConstructor({
  documentName,
  templateName,
  tags,
  initialConfiguration,
  sessionKey,
  isSaving = false,
  isTemplateFileMissing = false,
  canBackFromFirstStep = true,
  onTemplateNameChange,
  onTemplateFileChange,
  onCancel,
  onBack,
  onComplete,
}: TemplateConstructorProps) {
  const { data: schema, isLoading, isError } = useConstructorSchema()
  const initialize = useConstructorStore((state) => state.initialize)
  const currentStep = useConstructorStore((state) => state.currentStep)
  const previousStep = useConstructorStore((state) => state.previousStep)
  const nextStep = useConstructorStore((state) => state.nextStep)
  const setStep = useConstructorStore((state) => state.setStep)
  const calculateIncludes = useConstructorStore((state) => state.calculateIncludes)
  const config = useConstructorStore((state) => state.config)
  const tagTypes = useConstructorStore((state) => state.tagTypes)
  const appliedScenarioId = useConstructorStore((state) => state.appliedScenarioId)
  const requiredScalarMappings = useConstructorStore((state) => state.requiredScalarMappings)
  const requiredTableSources = useConstructorStore((state) => state.requiredTableSources)
  const [validationErrors, setValidationErrors] = useState<string[]>([])

  const schemaKeys = useMemo(() => Object.keys(schema ?? {}), [schema])
  const formattedJson = useMemo(() => JSON.stringify(config, null, 2), [config])
  const mappingProgress = useMemo(() => {
    const activeTags = Object.entries(tagTypes)
      .filter(([, type]) => type !== 'reserved')
      .map(([tag]) => tag)
    const totalTags = activeTags.length
    const scalarTags = Object.keys(config.Mapping.Scalars)
    const tableTags = Object.values(config.Mapping.Tables).flatMap((table) => Object.keys(table.RowMapping))
    const mappedTableTags = activeTags.filter((tag) => tableTags.includes(getTableRowTagName(tag)))
    const assignedTags = new Set([...scalarTags, ...mappedTableTags].filter((tag) => activeTags.includes(tag))).size

    return { assignedTags, totalTags, isComplete: assignedTags >= totalTags }
  }, [config.Mapping.Scalars, config.Mapping.Tables, tagTypes])

  useEffect(() => {
    initialize({
      tags,
      config: initialConfiguration,
      defaultEntity: schemaKeys[0] ?? '',
      sessionKey,
    })
  }, [initialize, initialConfiguration, schemaKeys, sessionKey, tags])

  const goToStep = (step: 1 | 2 | 3 | 4) => {
    if (step === 4 && !mappingProgress.isComplete) return
    if (step === 4) calculateIncludes(schema)
    setStep(step)
  }

  const handleNext = () => {
    if (currentStep === 3 && !mappingProgress.isComplete) return
    if (currentStep === 3) calculateIncludes(schema)
    nextStep()
  }

  const handleComplete = () => {
    calculateIncludes(schema)
    const nextConfig = useConstructorStore.getState().config
    const errors = validateTemplateConfiguration(nextConfig, schema, {
      appliedScenarioId,
      requiredScalarMappings,
      requiredTableSources,
    })
    setValidationErrors(errors)
    if (errors.length > 0) return
    onComplete(nextConfig)
  }

  return (
    <div className="grid min-h-0 grid-cols-1 gap-[clamp(12px,1vw,18px)] lg:grid-cols-[minmax(0,1fr)_clamp(300px,24%,430px)]">
      <section className="ui-surface flex min-h-0 flex-col px-5 py-[clamp(20px,1.8vw,30px)]">
        <div className="mb-[clamp(20px,2.5vh,32px)] grid shrink-0 grid-cols-[auto_1fr_auto] items-center gap-3">
          <Button variant="ghost" size="sm" onClick={onCancel}>
            <ArrowLeft size={14} />
            Скасувати
          </Button>

          <div className="flex justify-center gap-4">
            {stepLabels.map((label, index) => {
              const step = (index + 1) as 1 | 2 | 3 | 4
              const isActive = currentStep >= step
              const canOpen = step < 4 || mappingProgress.isComplete

              return (
                <button
                  key={label}
                  type="button"
                  disabled={!canOpen}
                  onClick={() => goToStep(step)}
                  className={cn(
                    'flex h-[58px] w-[58px] items-center justify-center rounded-full border-2 text-3xl font-extrabold leading-none transition disabled:cursor-not-allowed',
                    isActive && currentStep > step
                      ? 'border-[var(--color-accent)] bg-[var(--color-accent)] text-white shadow-[var(--shadow-ui)]'
                      : isActive
                        ? 'border-[var(--color-accent)] bg-[var(--color-surface)] text-[var(--color-accent)]'
                        : 'border-transparent bg-slate-50 text-[var(--color-accent)] opacity-60',
                  )}
                >
                  {label}
                </button>
              )
            })}
          </div>

          <div className="w-[92px] text-right text-xs font-black uppercase text-slate-400">
            {currentStep === 3 && `${mappingProgress.assignedTags}/${mappingProgress.totalTags}`}
          </div>
        </div>

        {isLoading && (
          <div className="flex min-h-0 flex-1 items-center justify-center text-[var(--color-primary)]">
            <Loader2 className="mr-2 animate-spin" size={20} />
            Завантаження схеми даних
          </div>
        )}

        {isError && (
          <div className="rounded-[var(--radius-ui-sm)] border border-[var(--color-danger)] bg-[var(--color-danger-soft)] p-4 text-sm font-bold text-[var(--color-danger)]">
            Не вдалося отримати схему з `/api/constructor/schema`. Перевірте, що backend запущений.
          </div>
        )}

        {!isLoading && !isError && (
          <>
            <div className="min-h-0">
              {currentStep === 1 && <TagMarkupStep />}
              {currentStep === 2 && <DataSourcesStep schema={schema} />}
              {currentStep === 3 && <MappingStep schema={schema} />}
              {currentStep === 4 && (
                <ReviewStep
                  documentName={documentName}
                  templateName={templateName}
                  validationErrors={validationErrors}
                  isTemplateFileMissing={isTemplateFileMissing}
                  onTemplateNameChange={onTemplateNameChange}
                  onTemplateFileChange={onTemplateFileChange}
                  onBack={onBack}
                  onSave={handleComplete}
                  isSaving={isSaving}
                  showDocumentBackButton={canBackFromFirstStep}
                />
              )}
            </div>

            {currentStep < 4 && (
              <div className="mt-[clamp(18px,2vh,28px)] flex shrink-0 justify-between">
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={currentStep === 1 ? onBack : previousStep}
                  disabled={currentStep === 1 && !canBackFromFirstStep}
                >
                  <ArrowLeft size={14} />
                  Назад
                </Button>
                <Button
                  variant="success"
                  size="sm"
                  onClick={handleNext}
                  disabled={currentStep === 3 && !mappingProgress.isComplete}
                >
                  Продовжити
                </Button>
              </div>
            )}
          </>
        )}
      </section>

      <aside className="ui-json-panel sticky top-4 flex max-h-[calc(100vh-2rem)] min-h-0 self-start overflow-hidden p-5 shadow-[var(--shadow-ui)]">
        <h3 className="text-xs font-extrabold uppercase text-[var(--color-success-soft)]">Live JSON</h3>
        <pre className="json-scrollbar mt-4 max-w-full flex-1 overflow-auto whitespace-pre-wrap break-words font-mono text-[11px] leading-4 text-[var(--color-success-soft)] [overflow-wrap:anywhere]">
          {formattedJson}
        </pre>
      </aside>
    </div>
  )
}
