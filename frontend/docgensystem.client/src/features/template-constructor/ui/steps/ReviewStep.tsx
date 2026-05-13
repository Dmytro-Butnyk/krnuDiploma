import { ArrowLeft, CheckCircle2, Loader2 } from 'lucide-react'
import { Button } from '../../../../shared/ui/Button'

type Props = {
  documentName: string
  templateName: string
  onTemplateNameChange: (name: string) => void
  onBack: () => void
  onSave: () => void
  isSaving: boolean
}

export function ReviewStep({
  documentName,
  templateName,
  onTemplateNameChange,
  onBack,
  onSave,
  isSaving,
}: Props) {
  return (
    <div className="flex min-h-[515px] flex-col items-center">
      <button
        className="self-start rounded-full border border-blue-200 px-4 py-1 text-xs font-semibold text-blue-700"
        onClick={onBack}
      >
        <ArrowLeft className="mr-1 inline" size={13} />
        Повернутися до документу
      </button>

      <div className="mt-20 flex w-full max-w-[420px] flex-col items-center text-center">
        <div className="flex h-16 w-16 items-center justify-center rounded-full bg-lime-500 text-white">
          <CheckCircle2 size={40} />
        </div>
        <h3 className="mt-5 text-2xl font-black uppercase text-blue-700">Конфігурація успішна!</h3>
        <div className="mt-3 rounded-full border border-blue-100 bg-white px-4 py-1 text-xs font-semibold text-blue-700">
          {documentName}
        </div>

        <label className="mt-8 block w-full text-left">
          <span className="mb-2 block text-xs font-bold uppercase tracking-wide text-slate-500">
            Назва шаблону в системі
          </span>
          <input
            className="w-full rounded-lg border border-slate-200 px-4 py-2 text-sm outline-none focus:border-blue-500"
            value={templateName}
            onChange={(event) => onTemplateNameChange(event.target.value)}
            placeholder={documentName.replace(/\.docx$/i, '')}
          />
        </label>

        <Button
          className="mt-6 w-64 rounded-full bg-blue-600 py-3 text-base hover:bg-blue-700"
          onClick={onSave}
          disabled={isSaving}
        >
          {isSaving && <Loader2 size={18} className="animate-spin" />}
          Зберегти
        </Button>
      </div>
    </div>
  )
}
