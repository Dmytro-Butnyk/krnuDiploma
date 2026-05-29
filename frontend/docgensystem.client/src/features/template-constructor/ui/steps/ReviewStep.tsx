import { ArrowLeft, CheckCircle2, Loader2 } from 'lucide-react'
import { Button } from '../../../../shared/ui/Button'

type Props = {
  documentName: string
  templateName: string
  onTemplateNameChange: (name: string) => void
  onBack: () => void
  onSave: () => void
  isSaving: boolean
  showDocumentBackButton?: boolean
}

export function ReviewStep({
  documentName,
  templateName,
  onTemplateNameChange,
  onBack,
  onSave,
  isSaving,
  showDocumentBackButton = true,
}: Props) {
  return (
    <div className="custom-scrollbar flex h-full min-h-0 w-full flex-col items-center overflow-y-auto overflow-x-hidden px-1 pb-6">
      {showDocumentBackButton && (
        <Button variant="secondary" size="sm" className="max-w-full self-start" onClick={onBack}>
          <ArrowLeft size={13} />
          Повернутися до документу
        </Button>
      )}

      <div className="mt-[clamp(28px,8vh,96px)] flex w-full max-w-[460px] shrink-0 flex-col items-center text-center">
        <div className="flex h-16 w-16 items-center justify-center rounded-full bg-[var(--color-success-soft)] text-white">
          <CheckCircle2 size={40} />
        </div>
        <h3 className="ui-step-title mt-5">Конфігурація успішна!</h3>
        <div className="mt-4 max-w-full overflow-hidden text-ellipsis whitespace-nowrap rounded-full border border-[var(--color-primary)] bg-white px-5 py-2 text-sm font-bold text-[var(--color-primary)]">
          {documentName}
        </div>

        <label className="mt-8 block w-full text-left">
          <span className="ui-label mb-2 block">Назва шаблону в системі</span>
          <input
            className="ui-input w-full px-4 py-3 text-base font-bold"
            value={templateName}
            onChange={(event) => onTemplateNameChange(event.target.value)}
            placeholder={documentName.replace(/\.docx$/i, '')}
          />
        </label>

        <Button size="pill" className="mt-6 w-64 max-w-full" onClick={onSave} disabled={isSaving}>
          {isSaving && <Loader2 size={18} className="animate-spin" />}
          Зберегти
        </Button>
      </div>
    </div>
  )
}
