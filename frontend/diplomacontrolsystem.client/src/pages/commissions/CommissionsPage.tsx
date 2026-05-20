import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, Plus, X } from 'lucide-react'
import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import {
  createDiplomaExaminationCommission,
  deleteDiplomaExaminationCommission,
  updateDiplomaExaminationCommission,
} from '../../features/commissions/api/commissionsApi'
import type {
  DiplomaExaminationCommissionResponse,
} from '../../features/commissions/api/types'
import {
  commissionOptionsQuery,
  commissionQueryKeys,
  commissionsQuery,
} from '../../features/commissions/model/commissionsQueries'
import { useAuth } from '../../features/auth/model/useAuth'
import type { AcademicYearOverviewResponse, EducationLevel, EntityId } from '../../features/groups/api/types'
import { academicYearsQuery } from '../../features/groups/model/groupsQueries'
import { getApiErrorMessage, getApiErrorMessages } from '../../shared/api/errorMessage'
import { ConfirmDialog } from '../../shared/ui/ConfirmDialog'
import { useToast } from '../../shared/ui/toast/ToastContext'

const educationOptions: Array<{ value: EducationLevel; label: string }> = [
  { value: 'Bachelor', label: 'Бакалаври' },
  { value: 'Master', label: 'Магістри' },
]

type HeadMode = 'teacher' | 'person'

interface CommissionFormState {
  orderNumber: string
  groupIds: string[]
  headMode: HeadMode
  headTeacherId: string
  headPersonaName: string
  headPersonaPosition: string
  firstMemberTeacherId: string
  secondMemberTeacherId: string
  thirdMemberTeacherId: string
  secretaryId: string
  startDate: string
  endDate: string
}

const emptyForm: CommissionFormState = {
  orderNumber: '',
  groupIds: [''],
  headMode: 'teacher',
  headTeacherId: '',
  headPersonaName: '',
  headPersonaPosition: '',
  firstMemberTeacherId: '',
  secondMemberTeacherId: '',
  thirdMemberTeacherId: '',
  secretaryId: '',
  startDate: '',
  endDate: '',
}

function makeDefaultCommissionForm(defenseYear: string): CommissionFormState {
  return {
    ...emptyForm,
    startDate: `${defenseYear}-01-01`,
    endDate: `${defenseYear}-12-31`,
  }
}

function asString(id: EntityId | undefined | null) {
  return id === undefined || id === null ? '' : String(id)
}

function makePath(path: string, educationLevel: EducationLevel) {
  return `${path}?level=${educationLevel}`
}

function displayDate(value: string) {
  const [year, month, day] = value.split('-')
  return day && month && year ? `${day}.${month}.${year}` : value
}

function currentUkraineYear() {
  return Number(new Intl.DateTimeFormat('en-US', { timeZone: 'Europe/Kyiv', year: 'numeric' }).format(new Date()))
}

function isArchivedDefenseYear(defenseYear: string) {
  return Number(defenseYear) < currentUkraineYear()
}

function activeYears(years: AcademicYearOverviewResponse[]) {
  return years.filter((year) => !isArchivedDefenseYear(year.defenseYear))
}

function commissionHeadName(commission: DiplomaExaminationCommissionResponse) {
  return commission.head.teacher?.fullName ?? commission.head.person?.fullName ?? 'Не призначено'
}

function commissionHeadPosition(commission: DiplomaExaminationCommissionResponse) {
  return commission.head.teacher?.position ?? commission.head.person?.position ?? ''
}

function formFromCommission(commission?: DiplomaExaminationCommissionResponse): CommissionFormState {
  if (!commission) {
    return emptyForm
  }

  return {
    orderNumber: commission.orderNumber,
    groupIds: commission.groups.length > 0 ? commission.groups.map((group) => asString(group.id)) : [''],
    headMode: commission.head.teacher ? 'teacher' : 'person',
    headTeacherId: asString(commission.head.teacher?.id),
    headPersonaName: commission.head.person?.fullName ?? '',
    headPersonaPosition: commission.head.person?.position ?? '',
    firstMemberTeacherId: asString(commission.members[0]?.teacherId),
    secondMemberTeacherId: asString(commission.members[1]?.teacherId),
    thirdMemberTeacherId: asString(commission.members[2]?.teacherId),
    secretaryId: asString(commission.secretary.id),
    startDate: commission.startDate,
    endDate: commission.endDate,
  }
}

function SectionMessage({ children }: { children: string }) {
  return (
    <div className="rounded-[18px] bg-white/70 px-6 py-5 text-center text-lg font-bold text-slate-500 shadow-sm">
      {children}
    </div>
  )
}

function ErrorMessage({ error }: { error: unknown }) {
  return (
    <div className="rounded-[18px] border border-red-200 bg-red-50 px-6 py-5 text-center text-lg font-bold text-red-500">
      {getApiErrorMessage(error)}
    </div>
  )
}

function EducationSwitch({
  value,
  onChange,
}: {
  value: EducationLevel
  onChange: (value: EducationLevel) => void
}) {
  return (
    <div className="flex rounded-full bg-white/70 p-2 shadow-[0_2px_10px_rgba(71,85,105,0.22)]">
      {educationOptions.map((option) => (
        <button
          key={option.value}
          type="button"
          onClick={() => onChange(option.value)}
          className={[
            'h-14 rounded-full px-10 text-2xl font-bold transition',
            value === option.value
              ? 'border border-orange-500 bg-white text-orange-600'
              : 'text-slate-500 hover:bg-white/70',
          ].join(' ')}
        >
          {option.label}
        </button>
      ))}
    </div>
  )
}

function TopControls({
  educationLevel,
  onEducationChange,
  onCreateCommission,
  canCreate,
}: {
  educationLevel: EducationLevel
  onEducationChange: (value: EducationLevel) => void
  onCreateCommission: () => void
  canCreate: boolean
}) {
  return (
    <div className="flex items-center justify-center gap-24">
      <EducationSwitch value={educationLevel} onChange={onEducationChange} />
      {canCreate && (
        <button
          type="button"
          onClick={onCreateCommission}
          className="inline-flex h-16 items-center gap-3 rounded-full bg-blue-600 px-12 text-2xl font-bold text-white shadow-sm transition hover:bg-blue-700"
        >
          <Plus size={24} />
          Створити комісію
        </button>
      )}
    </div>
  )
}

function YearCards({
  years,
  educationLevel,
}: {
  years: AcademicYearOverviewResponse[]
  educationLevel: EducationLevel
}) {
  return (
    <div className="grid grid-cols-3 gap-8">
      {years.map((item, index) => (
        <Link
          key={item.defenseYear}
          to={makePath(`/commissions/${item.defenseYear}`, educationLevel)}
          className={[
            'min-h-40 rounded-[18px] p-8 shadow-sm transition hover:-translate-y-0.5',
            index === 0 ? 'bg-blue-600 text-white' : 'bg-white/70 text-blue-300',
          ].join(' ')}
        >
          <p className="text-sm font-bold uppercase opacity-70">Навчальний рік</p>
          <p className="mt-2 text-5xl font-bold">{item.year}</p>
          <p className="mt-8 text-sm font-bold">{isArchivedDefenseYear(item.defenseYear) ? 'Архів' : 'ДЕК'}</p>
        </Link>
      ))}
    </div>
  )
}

function YearTabs({
  years,
  activeDefenseYear,
  educationLevel,
}: {
  years: AcademicYearOverviewResponse[]
  activeDefenseYear: string
  educationLevel: EducationLevel
}) {
  const visibleYears = activeYears(years)

  return (
    <div className="flex h-16 items-center gap-4 rounded-[18px] bg-white/70 px-6 shadow-sm">
      <Link to={makePath('/commissions', educationLevel)} aria-label="Назад до років" className="text-slate-500">
        <ArrowLeft size={34} />
      </Link>
      {visibleYears.map((year) => (
        <Link
          key={year.defenseYear}
          to={makePath(`/commissions/${year.defenseYear}`, educationLevel)}
          className={[
            'rounded-full px-5 py-2 text-lg font-bold transition',
            year.defenseYear === activeDefenseYear
              ? 'border border-blue-600 bg-white text-blue-600'
              : 'text-slate-500 hover:bg-white',
          ].join(' ')}
        >
          {year.year}
        </Link>
      ))}
    </div>
  )
}

function CommissionSidebar({
  commissions,
  selectedCommissionId,
  educationLevel,
  defenseYear,
}: {
  commissions: DiplomaExaminationCommissionResponse[]
  selectedCommissionId?: EntityId
  educationLevel: EducationLevel
  defenseYear: string
}) {
  return (
    <aside className="min-h-[520px] rounded-[22px] bg-white/65 p-8 shadow-sm">
      <h2 className="text-xl font-bold uppercase text-slate-500">Комісія</h2>
      <div className="mt-8 space-y-3">
        {commissions.map((commission) => (
          <Link
            key={commission.id}
            to={makePath(`/commissions/${defenseYear}/${commission.id}`, educationLevel)}
            className={[
              'block rounded-2xl px-5 py-4 text-2xl font-bold transition',
              asString(commission.id) === asString(selectedCommissionId)
                ? 'bg-blue-600 text-white'
                : 'text-slate-500 hover:bg-white',
            ].join(' ')}
          >
            ЕК №{commission.orderNumber}
          </Link>
        ))}
      </div>
    </aside>
  )
}

function CommissionDetails({
  commission,
  onEdit,
  onDelete,
}: {
  commission: DiplomaExaminationCommissionResponse
  onEdit: () => void
  onDelete: () => void
}) {
  return (
    <article className="rounded-[22px] bg-white/65 p-9 shadow-sm">
      <div className="flex items-start justify-between gap-8">
        <h1 className="text-4xl font-bold uppercase text-blue-600">ЕК №{commission.orderNumber}</h1>
        <div className="flex gap-3">
          <button
            type="button"
            onClick={onEdit}
            className="h-11 rounded-full border border-blue-600 px-8 font-bold text-blue-600 transition hover:bg-blue-50"
          >
            Змінити
          </button>
          <button
            type="button"
            onClick={onDelete}
            className="h-11 rounded-full border border-red-500 px-8 font-bold text-red-500 transition hover:bg-red-50"
          >
            Видалити
          </button>
        </div>
      </div>

      <div className="mt-10 grid grid-cols-[1fr_360px] gap-12">
        <div className="space-y-8">
          <section>
            <h2 className="text-sm font-bold uppercase text-slate-500">Голова комісії</h2>
            <p className="mt-5 text-2xl font-bold text-slate-800">{commissionHeadName(commission)}</p>
            {commissionHeadPosition(commission) && (
              <p className="mt-2 max-w-[440px] text-base font-bold text-slate-500">
                {commissionHeadPosition(commission)}
              </p>
            )}
          </section>

          <section>
            <h2 className="text-sm font-bold uppercase text-slate-500">Члени комісії</h2>
            <div className="mt-5 space-y-5">
              {commission.members.map((member, index) => (
                <div key={member.teacherId} className="grid grid-cols-[34px_1fr] gap-3">
                  <span className="text-2xl font-bold text-slate-800">{index + 1}.</span>
                  <div>
                    <p className="text-2xl font-bold text-slate-800">{member.fullName}</p>
                    <p className="mt-2 max-w-[440px] text-base font-bold text-slate-500">{member.position}</p>
                  </div>
                </div>
              ))}
            </div>
          </section>

          <section>
            <h2 className="text-sm font-bold uppercase text-slate-500">Секретар комісії</h2>
            <p className="mt-5 text-2xl font-bold text-slate-800">{commission.secretary.fullName}</p>
          </section>
        </div>

        <div className="space-y-8">
          <section className="rounded-[18px] bg-white p-7 shadow-sm">
            <h2 className="text-sm font-bold uppercase text-slate-500">Термін роботи</h2>
            <div className="mt-8 grid grid-cols-[1fr_auto] gap-y-6 text-lg font-bold">
              <span className="text-slate-500">Початок роботи</span>
              <span className="text-orange-600">{displayDate(commission.startDate)}</span>
              <span className="text-slate-500">Кінець роботи</span>
              <span className="text-orange-600">{displayDate(commission.endDate)}</span>
            </div>
          </section>

          <section className="rounded-[18px] bg-orange-500 p-7 text-white shadow-sm">
            <h2 className="text-sm font-bold uppercase">Група</h2>
            <div className="mt-8 grid grid-cols-[1fr_auto] gap-x-8 text-xl font-bold">
              <span>ДЕК призначено для групи</span>
              <div className="space-y-5 text-right text-2xl">
                {commission.groups.map((group) => (
                  <p key={group.id}>{group.name}</p>
                ))}
              </div>
            </div>
          </section>
        </div>
      </div>
    </article>
  )
}

function FormInput({
  label,
  value,
  onChange,
  type = 'text',
}: {
  label: string
  value: string
  onChange: (value: string) => void
  type?: string
}) {
  return (
    <label className="grid max-w-[520px] grid-cols-[170px_1fr] items-center gap-6 text-lg font-bold text-slate-700">
      <span>{label}</span>
      <input
        type={type}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="h-12 rounded-xl border border-slate-300 bg-transparent px-4 outline-none focus:border-blue-500"
      />
    </label>
  )
}

function SelectField({
  label,
  value,
  onChange,
  children,
}: {
  label: string
  value: string
  onChange: (value: string) => void
  children: ReactNode
}) {
  return (
    <label className="grid grid-cols-[170px_1fr] items-center gap-6 text-lg font-bold text-slate-700">
      <span>{label}</span>
      <select
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="h-12 rounded-xl border border-slate-300 bg-transparent px-4 outline-none focus:border-blue-500"
      >
        {children}
      </select>
    </label>
  )
}

function CommissionFormDialog({
  mode,
  secretaryEmail,
  educationLevel,
  defenseYear,
  commission,
  onClose,
  onSuccess,
}: {
  mode: 'create' | 'edit'
  secretaryEmail: string
  educationLevel: EducationLevel
  defenseYear: string
  commission?: DiplomaExaminationCommissionResponse
  onClose: () => void
  onSuccess: (commission: DiplomaExaminationCommissionResponse) => void
}) {
  const queryClient = useQueryClient()
  const { showError, showSuccess } = useToast()
  const optionsQuery = useQuery(commissionOptionsQuery(secretaryEmail, educationLevel, defenseYear, commission?.id))
  const [form, setForm] = useState<CommissionFormState>(() =>
    commission ? formFromCommission(commission) : makeDefaultCommissionForm(defenseYear),
  )
  const options = optionsQuery.data
  const selectedGroupIds = form.groupIds.filter(Boolean)
  const selectedMemberIds = [
    form.firstMemberTeacherId,
    form.secondMemberTeacherId,
    form.thirdMemberTeacherId,
  ].filter(Boolean)
  useEffect(() => {
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'

    return () => {
      document.body.style.overflow = previousOverflow
    }
  }, [])
  const mutation = useMutation({
    mutationFn: () => {
      const request = {
        secretaryEmail,
        secretaryId: form.secretaryId,
        orderNumber: form.orderNumber.trim(),
        educationLevel,
        defenseYear,
        groupIds: form.groupIds.filter(Boolean),
        headTeacherId: form.headMode === 'teacher' ? form.headTeacherId || null : null,
        headPersonaName: form.headMode === 'person' ? form.headPersonaName.trim() || null : null,
        headPersonaPosition: form.headMode === 'person' ? form.headPersonaPosition.trim() || null : null,
        firstMemberTeacherId: form.firstMemberTeacherId,
        secondMemberTeacherId: form.secondMemberTeacherId,
        thirdMemberTeacherId: form.thirdMemberTeacherId,
        startDate: form.startDate,
        endDate: form.endDate,
      }

      return mode === 'create'
        ? createDiplomaExaminationCommission(request)
        : updateDiplomaExaminationCommission(commission?.id ?? '', request)
    },
    onSuccess: async (response) => {
      await queryClient.invalidateQueries({ queryKey: commissionQueryKeys.all })
      showSuccess()
      onSuccess(response)
    },
    onError: (error) => showError(getApiErrorMessages(error)),
  })

  const updateForm = (patch: Partial<CommissionFormState>) => setForm((current) => ({ ...current, ...patch }))
  const updateGroup = (index: number, value: string) =>
    updateForm({ groupIds: form.groupIds.map((groupId, itemIndex) => (itemIndex === index ? value : groupId)) })
  const removeGroup = (index: number) =>
    updateForm({ groupIds: form.groupIds.length > 1 ? form.groupIds.filter((_, itemIndex) => itemIndex !== index) : [''] })
  const addGroup = () => {
    if (options && form.groupIds.length >= options.groups.length) {
      return
    }

    updateForm({ groupIds: [...form.groupIds, ''] })
  }
  const validate = () => {
    const messages: string[] = []
    const memberIds = [form.firstMemberTeacherId, form.secondMemberTeacherId, form.thirdMemberTeacherId]
    const selectedGroups = form.groupIds.filter(Boolean)

    if (!form.orderNumber.trim()) {
      messages.push('Вкажіть № комісії.')
    }
    if (selectedGroups.length === 0) {
      messages.push('Оберіть хоча б одну групу.')
    }
    if (form.headMode === 'teacher' && !form.headTeacherId) {
      messages.push('Оберіть голову комісії.')
    }
    if (form.headMode === 'person' && (!form.headPersonaName.trim() || !form.headPersonaPosition.trim())) {
      messages.push('Заповніть ПІБ та посаду запрошеної голови комісії.')
    }
    if (memberIds.some((id) => !id)) {
      messages.push('Оберіть трьох членів комісії.')
    }
    if (!form.secretaryId) {
      messages.push('Оберіть секретаря комісії.')
    }
    if (!form.startDate || !form.endDate) {
      messages.push('Вкажіть термін роботи ДЕК.')
    }
    if (form.startDate && form.endDate && form.endDate < form.startDate) {
      messages.push('Кінець роботи має бути не раніше початку.')
    }
    if (memberIds.filter(Boolean).length !== new Set(memberIds.filter(Boolean)).size) {
      messages.push('Члени комісії мають бути різними викладачами.')
    }
    if (form.headMode === 'teacher' && form.headTeacherId && memberIds.includes(form.headTeacherId)) {
      messages.push('Голова та члени комісії мають бути різними людьми.')
    }

    return messages
  }
  const submit = () => {
    const messages = validate()
    if (messages.length > 0) {
      showError(messages)
      return
    }

    mutation.mutate()
  }

  return (
    <div className="fixed inset-0 z-40 overflow-hidden bg-[#dcecff]/80 px-6 py-10 backdrop-blur-sm">
      <section className="mx-auto max-h-[calc(100vh-80px)] max-w-[1280px] overflow-y-auto rounded-[28px] bg-white/80 p-10 shadow-xl">
        <div className="flex items-start justify-between">
          <h2 className="text-4xl font-bold uppercase text-blue-600">Створення екзаменаційної комісії</h2>
          <button type="button" onClick={onClose} aria-label="Закрити" className="text-red-500">
            <X size={42} />
          </button>
        </div>

        {optionsQuery.isLoading && <div className="mt-10"><SectionMessage>Завантажуємо дані форми...</SectionMessage></div>}
        {optionsQuery.error && <div className="mt-10"><ErrorMessage error={optionsQuery.error} /></div>}

        {options && (
          <div className="mt-10 max-w-[1120px] space-y-10">
            <section className="space-y-6">
              <h3 className="text-sm font-bold uppercase text-slate-500">Загальна інформація</h3>
              <FormInput label="№ комісії" value={form.orderNumber} onChange={(orderNumber) => updateForm({ orderNumber })} />
              <label className="grid max-w-[520px] grid-cols-[170px_1fr] items-center gap-6 text-lg font-bold text-slate-700">
                <span>ОКР</span>
                <input
                  value={educationOptions.find((item) => item.value === educationLevel)?.label ?? educationLevel}
                  disabled
                  className="h-12 rounded-xl border border-slate-300 bg-transparent px-4 text-slate-500"
                />
              </label>
            </section>

            <section className="space-y-5">
              <h3 className="text-sm font-bold uppercase text-slate-500">Прив’язка до групи</h3>
              {form.groupIds.map((groupId, index) => (
                <div key={index} className="grid max-w-[520px] grid-cols-[170px_1fr_48px] items-center gap-6">
                  <span className="text-lg font-bold text-slate-700">{index === 0 ? 'Група' : ''}</span>
                  <select
                    value={groupId}
                    onChange={(event) => updateGroup(index, event.target.value)}
                    className="h-12 rounded-xl border border-slate-300 bg-transparent px-4 text-lg font-bold outline-none focus:border-blue-500"
                  >
                    <option value="">Оберіть групу</option>
                    {options.groups.map((group) => (
                      <option
                        key={group.id}
                        value={group.id}
                        disabled={selectedGroupIds.includes(asString(group.id)) && asString(group.id) !== groupId}
                      >
                        {group.name}
                      </option>
                    ))}
                  </select>
                  <button type="button" onClick={() => removeGroup(index)} className="text-red-500">
                    <X size={34} />
                  </button>
                </div>
              ))}
              <button
                type="button"
                onClick={addGroup}
                className="ml-[170px] h-11 rounded-full border-2 border-blue-600 px-10 text-lg font-bold text-blue-600 transition hover:bg-blue-50"
                disabled={options.groups.length === 0 || form.groupIds.length >= options.groups.length}
              >
                + Додати групу
              </button>
            </section>

            <section className="space-y-5">
              <h3 className="text-sm font-bold uppercase text-slate-500">Склад комісії</h3>
              <div className="grid grid-cols-[170px_1fr] items-start gap-6">
                <span className="pt-3 text-lg font-bold text-slate-700">Голова комісії</span>
                <div className="space-y-5">
                  <div className="inline-flex rounded-full bg-white/70 p-2 shadow-[0_2px_8px_rgba(71,85,105,0.22)]">
                    <button
                      type="button"
                      onClick={() => updateForm({ headMode: 'teacher' })}
                      className={[
                        'h-10 rounded-full px-6 text-lg font-bold transition',
                        form.headMode === 'teacher'
                          ? 'border border-blue-600 bg-white text-blue-600'
                          : 'text-slate-500 hover:bg-white',
                      ].join(' ')}
                    >
                      Викладацький склад
                    </button>
                    <button
                      type="button"
                      onClick={() => updateForm({ headMode: 'person' })}
                      className={[
                        'h-10 rounded-full px-6 text-lg font-bold transition',
                        form.headMode === 'person'
                          ? 'border border-blue-600 bg-white text-blue-600'
                          : 'text-slate-500 hover:bg-white',
                      ].join(' ')}
                    >
                      + Додати персону
                    </button>
                  </div>

                  {form.headMode === 'teacher' ? (
                    <select
                      value={form.headTeacherId}
                      onChange={(event) => updateForm({ headTeacherId: event.target.value })}
                      className="h-12 w-full rounded-xl border border-slate-300 bg-transparent px-4 text-lg font-bold outline-none focus:border-blue-500"
                    >
                      <option value="">Оберіть голову</option>
                      {options.teachers.map((teacher) => (
                        <option key={teacher.id} value={teacher.id} disabled={selectedMemberIds.includes(asString(teacher.id))}>
                          {teacher.fullName}
                        </option>
                      ))}
                    </select>
                  ) : (
                    <div className="space-y-4">
                      <input
                        value={form.headPersonaName}
                        onChange={(event) => updateForm({ headPersonaName: event.target.value })}
                        placeholder="Введіть повний ПІБ"
                        className="h-12 w-full rounded-xl border border-slate-300 bg-transparent px-4 text-lg font-bold outline-none placeholder:text-slate-400 focus:border-blue-500"
                      />
                      <input
                        value={form.headPersonaPosition}
                        onChange={(event) => updateForm({ headPersonaPosition: event.target.value })}
                        placeholder="Введіть посаду"
                        className="h-12 w-full rounded-xl border border-slate-300 bg-transparent px-4 text-lg font-bold outline-none placeholder:text-slate-400 focus:border-blue-500"
                      />
                    </div>
                  )}
                </div>
              </div>

              <SelectField label="1." value={form.firstMemberTeacherId} onChange={(firstMemberTeacherId) => updateForm({ firstMemberTeacherId })}>
                <option value="">Оберіть викладача</option>
                {options.teachers.map((teacher) => (
                  <option
                    key={teacher.id}
                    value={teacher.id}
                    disabled={
                      (selectedMemberIds.includes(asString(teacher.id)) &&
                        asString(teacher.id) !== form.firstMemberTeacherId) ||
                      (form.headMode === 'teacher' && asString(teacher.id) === form.headTeacherId)
                    }
                  >
                    {teacher.fullName}
                  </option>
                ))}
              </SelectField>
              <SelectField label="2." value={form.secondMemberTeacherId} onChange={(secondMemberTeacherId) => updateForm({ secondMemberTeacherId })}>
                <option value="">Оберіть викладача</option>
                {options.teachers.map((teacher) => (
                  <option
                    key={teacher.id}
                    value={teacher.id}
                    disabled={
                      (selectedMemberIds.includes(asString(teacher.id)) &&
                        asString(teacher.id) !== form.secondMemberTeacherId) ||
                      (form.headMode === 'teacher' && asString(teacher.id) === form.headTeacherId)
                    }
                  >
                    {teacher.fullName}
                  </option>
                ))}
              </SelectField>
              <SelectField label="3." value={form.thirdMemberTeacherId} onChange={(thirdMemberTeacherId) => updateForm({ thirdMemberTeacherId })}>
                <option value="">Оберіть викладача</option>
                {options.teachers.map((teacher) => (
                  <option
                    key={teacher.id}
                    value={teacher.id}
                    disabled={
                      (selectedMemberIds.includes(asString(teacher.id)) &&
                        asString(teacher.id) !== form.thirdMemberTeacherId) ||
                      (form.headMode === 'teacher' && asString(teacher.id) === form.headTeacherId)
                    }
                  >
                    {teacher.fullName}
                  </option>
                ))}
              </SelectField>
              <SelectField label="Секретар комісії" value={form.secretaryId} onChange={(secretaryId) => updateForm({ secretaryId })}>
                <option value="">Оберіть секретаря</option>
                {options.secretaries.map((secretary) => (
                  <option key={secretary.id} value={secretary.id}>
                    {secretary.fullName}
                  </option>
                ))}
              </SelectField>
            </section>

            <section className="space-y-5">
              <h3 className="text-sm font-bold uppercase text-slate-500">Термін роботи ДЕК</h3>
              <div className="grid max-w-[620px] grid-cols-2 gap-8">
                <label className="space-y-3 text-lg font-bold text-slate-700">
                  <span>Початок роботи</span>
                  <input
                    type="date"
                    value={form.startDate}
                    onChange={(event) => updateForm({ startDate: event.target.value })}
                    className="h-12 w-full rounded-xl border border-slate-300 bg-transparent px-4 outline-none focus:border-blue-500"
                  />
                </label>
                <label className="space-y-3 text-lg font-bold text-slate-700">
                  <span>Кінець роботи</span>
                  <input
                    type="date"
                    value={form.endDate}
                    onChange={(event) => updateForm({ endDate: event.target.value })}
                    className="h-12 w-full rounded-xl border border-slate-300 bg-transparent px-4 outline-none focus:border-blue-500"
                  />
                </label>
              </div>
            </section>

            <div className="flex justify-end gap-3">
              <button
                type="button"
                onClick={onClose}
                className="h-12 rounded-full border border-blue-600 px-8 text-lg font-bold text-blue-600"
              >
                Скасувати
              </button>
              <button
                type="button"
                onClick={submit}
                disabled={mutation.isPending}
                className="h-12 rounded-full border border-green-500 px-8 text-lg font-bold text-green-600 disabled:opacity-50"
              >
                {mode === 'create' ? 'Створити' : 'Зберегти'}
              </button>
            </div>
          </div>
        )}
      </section>
    </div>
  )
}

export function CommissionsPage() {
  const { defenseYear = '', commissionId } = useParams()
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { secretaryEmail } = useAuth()
  const { showError, showSuccess } = useToast()
  const educationLevel: EducationLevel = searchParams.get('level') === 'Master' ? 'Master' : 'Bachelor'
  const yearsQuery = useQuery(academicYearsQuery(secretaryEmail, educationLevel))
  const years = yearsQuery.data ?? []
  const selectedYear = years.find((item) => item.defenseYear === defenseYear) ?? years[0]
  const activeDefenseYear = defenseYear || selectedYear?.defenseYear || ''
  const commissionsListQuery = useQuery(commissionsQuery(secretaryEmail, educationLevel, activeDefenseYear))
  const commissions = useMemo(() => commissionsListQuery.data ?? [], [commissionsListQuery.data])
  const selectedCommission = useMemo(() => {
    if (commissions.length === 0) {
      return undefined
    }

    return commissions.find((commission) => asString(commission.id) === commissionId) ?? commissions[0]
  }, [commissionId, commissions])
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [isEditOpen, setIsEditOpen] = useState(false)
  const [commissionToDelete, setCommissionToDelete] = useState<DiplomaExaminationCommissionResponse | null>(null)
  const deleteMutation = useMutation({
    mutationFn: (commission: DiplomaExaminationCommissionResponse) =>
      deleteDiplomaExaminationCommission(commission.id, secretaryEmail),
    onSuccess: async () => {
      setCommissionToDelete(null)
      await queryClient.invalidateQueries({ queryKey: commissionQueryKeys.all })
      showSuccess()
      navigate(makePath(`/commissions/${activeDefenseYear}`, educationLevel), { replace: true })
    },
    onError: (error) => showError(getApiErrorMessages(error)),
  })
  const handleEducationChange = (nextLevel: EducationLevel) => {
    setSearchParams({ level: nextLevel })
  }
  const handleFormSuccess = (commission: DiplomaExaminationCommissionResponse) => {
    setIsCreateOpen(false)
    setIsEditOpen(false)
    navigate(makePath(`/commissions/${commission.defenseYear}/${commission.id}`, educationLevel))
  }

  return (
    <section className="space-y-12">
      <TopControls
        educationLevel={educationLevel}
        onEducationChange={handleEducationChange}
        onCreateCommission={() => setIsCreateOpen(true)}
        canCreate={Boolean(defenseYear)}
      />

      {yearsQuery.isLoading && <SectionMessage>Завантажуємо навчальні роки...</SectionMessage>}
      {yearsQuery.error && <ErrorMessage error={yearsQuery.error} />}
      {!yearsQuery.isLoading && !yearsQuery.error && years.length === 0 && (
        <SectionMessage>Навчальні роки для цього секретаря та ОКР не знайдено.</SectionMessage>
      )}

      {!defenseYear && years.length > 0 && <YearCards years={years} educationLevel={educationLevel} />}

      {defenseYear && selectedYear && (
        <div className="grid grid-cols-[320px_1fr] gap-7">
          <div className="space-y-8">
            <YearTabs years={years} activeDefenseYear={selectedYear.defenseYear} educationLevel={educationLevel} />
            <CommissionSidebar
              commissions={commissions}
              selectedCommissionId={selectedCommission?.id}
              educationLevel={educationLevel}
              defenseYear={selectedYear.defenseYear}
            />
          </div>

          {commissionsListQuery.isLoading && <SectionMessage>Завантажуємо комісії...</SectionMessage>}
          {commissionsListQuery.error && <ErrorMessage error={commissionsListQuery.error} />}
          {!commissionsListQuery.isLoading && !commissionsListQuery.error && commissions.length === 0 && (
            <SectionMessage>Для цього навчального року ще немає екзаменаційних комісій.</SectionMessage>
          )}
          {selectedCommission && (
            <CommissionDetails
              commission={selectedCommission}
              onEdit={() => setIsEditOpen(true)}
              onDelete={() => setCommissionToDelete(selectedCommission)}
            />
          )}
        </div>
      )}

      {isCreateOpen && activeDefenseYear && (
        <CommissionFormDialog
          mode="create"
          secretaryEmail={secretaryEmail}
          educationLevel={educationLevel}
          defenseYear={activeDefenseYear}
          onClose={() => setIsCreateOpen(false)}
          onSuccess={handleFormSuccess}
        />
      )}

      {isEditOpen && selectedCommission && (
        <CommissionFormDialog
          mode="edit"
          secretaryEmail={secretaryEmail}
          educationLevel={educationLevel}
          defenseYear={selectedCommission.defenseYear}
          commission={selectedCommission}
          onClose={() => setIsEditOpen(false)}
          onSuccess={handleFormSuccess}
        />
      )}

      {commissionToDelete && (
        <ConfirmDialog
          title="Видалення комісії"
          confirmLabel="Видалити"
          onConfirm={() => deleteMutation.mutate(commissionToDelete)}
          onCancel={() => setCommissionToDelete(null)}
        >
          Ви впевнені, що хочете видалити ЕК №{commissionToDelete.orderNumber}? Цю дію неможливо скасувати
        </ConfirmDialog>
      )}
    </section>
  )
}
