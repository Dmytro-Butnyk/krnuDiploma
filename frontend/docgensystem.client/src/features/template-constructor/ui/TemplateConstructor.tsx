import { ArrowLeft, Loader2 } from 'lucide-react'
import { useEffect, useMemo } from 'react'
import { useConstructorSchema } from '../../../entities/schema/api/schemaApi'
import { cn } from '../../../shared/lib/cn'
import { Button } from '../../../shared/ui/Button'
import { useConstructorStore } from '../model/store'
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
  isSaving?: boolean
  canBackFromFirstStep?: boolean
  onTemplateNameChange: (name: string) => void
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
  isSaving = false,
  canBackFromFirstStep = true,
  onTemplateNameChange,
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

  const schemaKeys = useMemo(() => Object.keys(schema ?? {}), [schema])
  const formattedJson = useMemo(() => JSON.stringify(config, null, 2), [config])
  const mappingProgress = useMemo(() => {
    const activeTags = Object.entries(tagTypes)
      .filter(([, type]) => type !== 'reserved')
      .map(([tag]) => tag)
    const totalTags = activeTags.length
    const scalarTags = Object.keys(config.Mapping.Scalars)
    const tableTags = Object.values(config.Mapping.Tables).flatMap((table) => Object.keys(table.RowMapping))
    const assignedTags = new Set([...scalarTags, ...tableTags].filter((tag) => activeTags.includes(tag))).size

    return { assignedTags, totalTags, isComplete: assignedTags >= totalTags }
  }, [config.Mapping.Scalars, config.Mapping.Tables, tagTypes])

  useEffect(() => {
    initialize({
      tags,
      config: initialConfiguration,
      defaultEntity: schemaKeys[0] ?? '',
    })
  }, [initialize, initialConfiguration, schemaKeys, tags])

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
    onComplete(useConstructorStore.getState().config)
  }

  return (
    <div className="grid grid-cols-1 gap-3 lg:grid-cols-[minmax(0,1fr)_282px]">
      <section className="min-h-[615px] rounded-xl bg-white px-5 py-5 shadow-sm ring-1 ring-blue-100">
        <div className="mb-7 grid grid-cols-[auto_1fr_auto] items-center gap-3">
          <Button variant="ghost" className="min-h-8 rounded-full px-4 py-1 text-xs" onClick={onCancel}>
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
                    'flex h-11 w-11 items-center justify-center rounded-full border-2 text-lg font-black transition disabled:cursor-not-allowed disabled:border-white disabled:bg-white disabled:text-orange-400/50',
                    isActive
                      ? 'border-orange-500 bg-orange-500 text-white hover:bg-orange-500 active:bg-orange-600'
                      : 'border-orange-500 bg-white text-orange-500 hover:bg-blue-50 active:bg-blue-100',
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
          <div className="flex min-h-[460px] items-center justify-center text-blue-700">
            <Loader2 className="mr-2 animate-spin" size={20} />
            Завантаження схеми даних
          </div>
        )}

        {isError && (
          <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm font-semibold text-red-700">
            Не вдалося отримати схему з `/api/constructor/schema`. Перевірте, що backend запущений.
          </div>
        )}

        {!isLoading && !isError && (
          <>
            {currentStep === 1 && <TagMarkupStep />}
            {currentStep === 2 && <DataSourcesStep schema={schema} />}
            {currentStep === 3 && <MappingStep schema={schema} />}
            {currentStep === 4 && (
              <ReviewStep
                documentName={documentName}
                templateName={templateName}
                onTemplateNameChange={onTemplateNameChange}
                onBack={onBack}
                onSave={handleComplete}
                isSaving={isSaving}
              />
            )}

            {currentStep < 4 && (
              <div className="mt-6 flex justify-between">
                <Button
                  variant="secondary"
                  className="min-h-8 rounded-full px-6 py-1 text-xs"
                  onClick={currentStep === 1 ? onBack : previousStep}
                  disabled={currentStep === 1 && !canBackFromFirstStep}
                >
                  <ArrowLeft size={14} />
                  Назад
                </Button>
                <Button
                  className="min-h-8 rounded-full px-7 py-1 text-xs"
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

      <aside className="min-h-[615px] rounded-xl bg-[#344356] p-5 shadow-sm">
        <h3 className="font-mono text-xs font-black uppercase text-emerald-400">Live JSON</h3>
        <pre className="json-scrollbar mt-4 h-[540px] overflow-auto whitespace-pre-wrap font-mono text-[11px] leading-4 text-emerald-300">
          {formattedJson}
        </pre>
      </aside>
    </div>
  )
}
