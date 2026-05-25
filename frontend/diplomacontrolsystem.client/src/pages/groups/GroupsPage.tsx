import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, Check, ChevronDown, ChevronUp, Maximize2, Minimize2, Plus, Upload, X } from 'lucide-react'
import { useEffect, useMemo, useState, type DragEvent, type ReactNode } from 'react'
import { Link, useLocation, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { useAuth } from '../../features/auth/model/useAuth'
import {
  createCommissionHead,
  createDiplomaExaminationCommission,
  deleteDiplomaExaminationCommission,
  updateDiplomaExaminationCommission,
} from '../../features/commissions/api/commissionsApi'
import type {
  CommissionHeadDto,
  DiplomaExaminationCommissionResponse,
} from '../../features/commissions/api/types'
import {
  commissionOptionsQuery,
  commissionQueryKeys,
  commissionsQuery,
} from '../../features/commissions/model/commissionsQueries'
import {
  addStudent,
  createGroup,
  deleteGroup,
  deleteStudent,
  updateDefenceResults,
  updateElectronicChecklist,
  updateGroup,
  updatePhysicalChecklist,
  updateQualificationWorkCharacteristics,
  updateStudentDefence,
  updateStudentName,
  updateStudentQualificationWork,
} from '../../features/groups/api/groupsApi'
import type {
  AcademicYearOverviewResponse,
  CharacteristicsDto,
  EducationLevel,
  ElectronicChecklistDto,
  EntityId,
  GroupDto,
  GroupStudentResponse,
  PhysicalChecklistDto,
  CreateGroupResponse,
  StudentDetailsResponse,
  UpdateGroupResponse,
} from '../../features/groups/api/types'
import {
  academicYearsQuery,
  groupStatisticsQuery,
  groupStudentsQuery,
  groupsQueryKeys,
  qualificationWorkOptionsQuery,
  studentDetailsQuery,
} from '../../features/groups/model/groupsQueries'
import { ApiError } from '../../shared/api/client'
import { getApiErrorMessage, getApiErrorMessages } from '../../shared/api/errorMessage'
import { ConfirmDialog } from '../../shared/ui/ConfirmDialog'
import { useToast } from '../../shared/ui/toast/ToastContext'

const educationOptions: Array<{ value: EducationLevel; label: string }> = [
  { value: 'Bachelor', label: 'Бакалаври' },
  { value: 'Master', label: 'Магістри' },
]

const physicalItems: Array<{ key: keyof PhysicalChecklistDto; label: string }> = [
  { key: 'hasStudentCard', label: 'Студентський квиток' },
  { key: 'hasGradeBook', label: 'Залікова книжка' },
  { key: 'hasCircular', label: 'Обхідний' },
  { key: 'hasSignedReview', label: 'Підписана рецензія' },
  { key: 'hasCopyOfBankReceipt', label: 'Копія сплати за бланк' },
  { key: 'hasExplanatoryNote', label: 'Пояснювальна записка' },
]

const electronicItems: Array<{ key: keyof ElectronicChecklistDto; label: string }> = [
  { key: 'hasRegulatoryControl', label: 'Нормоконтроль' },
  { key: 'hasExplanatoryNoteDoc', label: 'Пояснювальна записка .doc/.docx' },
  { key: 'hasExplanatoryNotePdf', label: 'Пояснювальна записка .pdf' },
  { key: 'hasPlagiarismReportPdf', label: 'Акт перевірки на плагіат .pdf' },
  { key: 'hasReviewDoc', label: 'Рецензія .doc/.docx' },
  { key: 'hasPresentationPpt', label: 'Презентація .ppt/.pptx' },
]

const characteristicItems: Array<{ key: keyof CharacteristicsDto; label: string }> = [
  { key: 'isResearchBased', label: 'Дослідного характеру' },
  { key: 'hasRealProjects', label: 'З реальними проектами та конструкторсько-технологічними розробками' },
  { key: 'isEcoFriendly', label: 'З раціонального природовикористання, ресурсозбереження та ох. навк. серед.' },
  { key: 'isEnterpriseOrdered', label: 'За замовленням підприємства' },
  { key: 'isComplexInteruniversity', label: 'Міжвузівські' },
  { key: 'isComplexInterdepartmental', label: 'Міжкафедральні' },
  { key: 'isComplexDepartmental', label: 'Кафедральні' },
  { key: 'isComplexProjectParticipant', label: 'Студ., які брали участь у комплексному проекті' },
  { key: 'isRecommendedForMaster', label: 'Рекомендовано в магістратуру' },
  { key: 'isRecommendedForImplementation', label: 'Рекомендовано ЕК до впровадження' },
  { key: 'isDefendedAtEnterprise', label: 'Захищено на підприємстві' },
]

const emptyPhysicalChecklist: PhysicalChecklistDto = {
  hasStudentCard: false,
  hasGradeBook: false,
  hasCircular: false,
  hasSignedReview: false,
  hasCopyOfBankReceipt: false,
  hasExplanatoryNote: false,
}

const emptyElectronicChecklist: ElectronicChecklistDto = {
  hasRegulatoryControl: false,
  hasExplanatoryNoteDoc: false,
  hasExplanatoryNotePdf: false,
  hasPlagiarismReportPdf: false,
  hasReviewDoc: false,
  hasPresentationPpt: false,
}

const emptyCharacteristics: CharacteristicsDto = {
  isResearchBased: false,
  hasRealProjects: false,
  isEcoFriendly: false,
  isEnterpriseOrdered: false,
  isComplexInteruniversity: false,
  isComplexInterdepartmental: false,
  isComplexDepartmental: false,
  isComplexProjectParticipant: false,
  isRecommendedForMaster: false,
  isRecommendedForImplementation: false,
  isDefendedAtEnterprise: false,
}

function asString(id: EntityId | undefined) {
  return id === undefined ? '' : String(id)
}

function makePath(path: string, educationLevel: EducationLevel) {
  return `${path}?level=${educationLevel}`
}

function displayDate(value: string) {
  const [year, month, day] = value.split('-')

  return day && month && year ? `${day}.${month}.${year}` : value
}

function isApiNotFound(error: unknown) {
  return error instanceof ApiError && error.status === 404
}

function currentDefenseYears() {
  const currentYear = currentUkraineYear()
  return [currentYear, currentYear + 1, currentYear + 2].map(String)
}

function currentUkraineYear() {
  return Number(new Intl.DateTimeFormat('en-US', { timeZone: 'Europe/Kyiv', year: 'numeric' }).format(new Date()))
}

function isArchivedDefenseYear(defenseYear: string) {
  return Number(defenseYear) < currentUkraineYear()
}

function academicStartYear(year: string) {
  const match = year.match(/^\d{4}/)

  return match ? Number(match[0]) : null
}

function isPastAcademicYear(year: string) {
  const startYear = academicStartYear(year)

  return startYear !== null && startYear < currentUkraineYear()
}

function activeYears(years: AcademicYearOverviewResponse[]) {
  return years.filter((year) => !isArchivedDefenseYear(year.defenseYear))
}

function countChecked<T extends object>(value: T | null, keys: Array<keyof T>) {
  if (!value) {
    return 0
  }

  return keys.filter((item) => Boolean(value[item])).length
}

function isPhysicalChecklistComplete(checklist: PhysicalChecklistDto | null) {
  return countChecked(checklist, physicalItems.map((item) => item.key)) === physicalItems.length
}

function isElectronicChecklistComplete(checklist: ElectronicChecklistDto | null) {
  return countChecked(checklist, electronicItems.map((item) => item.key)) === electronicItems.length
}

function isStudentAdmitted(student: GroupStudentResponse) {
  return isPhysicalChecklistComplete(student.physicalChecklist) && isElectronicChecklistComplete(student.electronicChecklist)
}

function getStatusClass(isAdmitted: boolean) {
  return isAdmitted ? 'border-green-500 text-green-600' : 'border-red-500 text-red-500'
}

function SectionMessage({ children }: { children: string }) {
  return (
    <div className="rounded-[18px] border border-blue-100 bg-white/75 px-6 py-5 text-center text-lg font-bold text-slate-500">
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
  onCreateGroup,
}: {
  educationLevel: EducationLevel
  onEducationChange: (value: EducationLevel) => void
  onCreateGroup: () => void
}) {
  return (
    <div className="flex items-center justify-center gap-24">
      <EducationSwitch value={educationLevel} onChange={onEducationChange} />
      <button
        type="button"
        onClick={onCreateGroup}
        className="inline-flex h-16 items-center gap-3 rounded-full bg-blue-600 px-12 text-2xl font-bold text-white shadow-sm transition hover:bg-blue-700"
      >
        <Plus size={24} />
        Створити групу
      </button>
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
      {years.map((item) => {
        const isPast = isPastAcademicYear(item.year)

        return (
          <Link
            key={item.defenseYear}
            to={makePath(`/groups/${item.defenseYear}`, educationLevel)}
            className={[
              'min-h-40 rounded-[18px] border p-8 shadow-sm transition hover:-translate-y-0.5 hover:border-blue-600 hover:bg-blue-600 hover:text-white hover:shadow-lg active:bg-blue-700 active:text-white',
              isPast
                ? 'border-white/50 bg-white/70 text-blue-300'
                : 'border-blue-200/60 bg-blue-100/80 text-blue-600',
            ].join(' ')}
          >
            <p className="text-sm font-bold uppercase opacity-70">Навчальний рік</p>
            <p className="mt-2 text-5xl font-bold">{item.year}</p>
            <p className="mt-8 text-sm font-bold">
              {isArchivedDefenseYear(item.defenseYear) ? 'Архів' : `${item.groups.length} групи`}
            </p>
          </Link>
        )
      })}
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
      <Link to={makePath('/groups', educationLevel)} aria-label="Назад до років" className="text-slate-500">
        <ArrowLeft size={34} />
      </Link>
      {visibleYears.map((year) => (
        <Link
          key={year.defenseYear}
          to={makePath(`/groups/${year.defenseYear}`, educationLevel)}
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

function GroupSidebar({
  title = 'Групи',
  groups,
  selectedGroupId,
  educationLevel,
  defenseYear,
  commission,
  isCommissionSelected = false,
  onCreateCommission,
}: {
  title?: string
  groups: GroupDto[]
  selectedGroupId?: EntityId
  educationLevel: EducationLevel
  defenseYear: string
  commission?: DiplomaExaminationCommissionResponse
  isCommissionSelected?: boolean
  onCreateCommission?: () => void
}) {
  return (
    <aside className="flex min-h-[520px] flex-col rounded-[22px] bg-white/65 p-8 shadow-sm">
      <div>
        <h2 className="text-xl font-bold uppercase text-slate-500">{title}</h2>
        <div className="mt-8 space-y-4">
          {groups.map((group) => (
            <Link
              key={group.id}
              to={makePath(`/groups/${defenseYear}/${group.id}`, educationLevel)}
              className={[
                'block rounded-2xl px-5 py-4 text-2xl font-bold transition',
                asString(selectedGroupId) === asString(group.id)
                  ? 'bg-blue-600 text-white'
                  : 'text-slate-500 hover:bg-white',
              ].join(' ')}
            >
              {group.name}
            </Link>
          ))}
          {groups.length === 0 && <p className="text-lg font-semibold text-slate-400">Груп ще немає</p>}
        </div>
      </div>

      <div className="mt-auto pt-10">
        <h2 className="text-xl font-bold uppercase text-slate-500">Комісія</h2>
        {commission ? (
          <Link
            to={makePath(`/groups/${defenseYear}/commission`, educationLevel)}
            className={[
              'mt-5 flex min-h-16 items-center justify-between rounded-2xl border-2 px-5 py-4 font-bold transition',
              isCommissionSelected
                ? 'border-orange-500 bg-white text-orange-600'
                : 'border-orange-500 bg-orange-500 text-white hover:bg-orange-600',
            ].join(' ')}
          >
            <span className="text-2xl">ЕК №{commission.orderNumber}</span>
            {!isCommissionSelected && <span className="text-sm">Переглянути відомості</span>}
          </Link>
        ) : (
          <button
            type="button"
            onClick={onCreateCommission}
            className="mt-5 h-14 w-full rounded-full border-2 border-green-500 text-xl font-bold text-green-600 transition hover:bg-green-500 hover:text-white disabled:opacity-50"
            disabled={!onCreateCommission}
          >
            + Створити ДЕК
          </button>
        )}
      </div>
    </aside>
  )
}

function commissionHeadName(commission: DiplomaExaminationCommissionResponse) {
  return commission.head.fullName || 'Не призначено'
}

function commissionHeadPosition(commission: DiplomaExaminationCommissionResponse) {
  return [commission.head.position, commission.head.company].filter(Boolean).join(', ')
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
    <article className="min-h-[520px] rounded-[22px] bg-white/65 p-9 shadow-sm">
      <div className="flex items-start justify-between gap-8">
        <h1 className="text-4xl font-bold uppercase text-blue-600">ЕК №{commission.orderNumber}</h1>
        <div className="flex gap-3">
          <button
            type="button"
            onClick={onEdit}
            className="h-11 rounded-full border-2 border-blue-600 px-8 font-bold text-blue-600 transition hover:bg-blue-600 hover:text-white"
          >
            Змінити
          </button>
          <button
            type="button"
            onClick={onDelete}
            className="h-11 rounded-full border-2 border-red-500 px-8 font-bold text-red-500 transition hover:bg-red-500 hover:text-white"
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
              <p className="mt-2 max-w-[520px] text-base font-bold text-slate-500">
                {commissionHeadPosition(commission)}
              </p>
            )}
            {commission.head.specialty && (
              <p className="mt-1 max-w-[520px] text-base font-bold text-slate-500">{commission.head.specialty}</p>
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
                    <p className="mt-2 max-w-[520px] text-base font-bold text-slate-500">{member.position}</p>
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
                {commission.groups.length === 0 && <p>Автоматично</p>}
              </div>
            </div>
          </section>
        </div>
      </div>
    </article>
  )
}

function StatusBadge({ admitted }: { admitted: boolean }) {
  return (
    <span className={`inline-flex rounded-full border px-4 py-2 font-bold ${getStatusClass(admitted)}`}>
      {admitted ? 'Допущено' : 'Не допущено'}
    </span>
  )
}

function GroupOverview({
  group,
  students,
  educationLevel,
  defenseYear,
  onEditGroup,
  onDeleteGroup,
  onAddStudent,
}: {
  group: GroupDto
  students: GroupStudentResponse[]
  educationLevel: EducationLevel
  defenseYear: string
  onEditGroup: () => void
  onDeleteGroup: () => void
  onAddStudent: () => void
}) {
  return (
    <article className="min-h-[520px] rounded-[22px] bg-white/65 p-9 shadow-sm">
      <div className="flex items-start justify-between gap-8">
        <h1 className="text-4xl font-bold text-blue-600">{group.name}</h1>
        <div className="flex gap-3">
          <button
            type="button"
            onClick={onDeleteGroup}
            className="h-11 rounded-full border-2 border-red-500 px-8 font-bold text-red-500 transition hover:bg-red-500 hover:text-white"
          >
            Видалити
          </button>
          <button
            type="button"
            onClick={onEditGroup}
            className="h-11 rounded-full border-2 border-blue-600 px-8 font-bold text-blue-600 transition hover:bg-blue-600 hover:text-white"
          >
            Змінити
          </button>
          <button
            type="button"
            onClick={onAddStudent}
            className="h-11 rounded-full border-2 border-green-500 px-8 font-bold text-green-600 transition hover:bg-green-500 hover:text-white"
          >
            Додати студента
          </button>
        </div>
      </div>

      <div className="mt-8 flex justify-end">
        <Link
          to={makePath(`/groups/${defenseYear}/${group.id}/admission`, educationLevel)}
          aria-label="Відкрити допуск до захисту"
          className="text-slate-500 transition hover:text-blue-600"
        >
          <Maximize2 size={32} />
        </Link>
      </div>

      <div className="mt-8 overflow-x-auto">
        <table className="w-full min-w-[760px] table-fixed border-collapse text-center text-slate-500">
          <thead className="border-b border-slate-300 text-sm font-bold">
            <tr>
              <th className="py-4">ID</th>
              <th className="py-4">ПІБ</th>
              <th className="py-4">Керівник</th>
              <th className="py-4">Матеріальні компоненти</th>
              <th className="py-4">Електронні компоненти</th>
              <th className="py-4">Статус</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200 text-sm font-semibold">
            {students.map((student, index) => {
              const physicalCount = countChecked(student.physicalChecklist, physicalItems.map((item) => item.key))
              const electronicCount = countChecked(
                student.electronicChecklist,
                electronicItems.map((item) => item.key),
              )
              const admitted = isStudentAdmitted(student)

              return (
                <tr key={student.id}>
                  <td className="py-5">{index + 1}</td>
                  <td>
                    <Link
                      to={makePath(`/groups/${defenseYear}/${group.id}/students/${student.id}`, educationLevel)}
                      className={admitted ? 'hover:text-blue-600' : 'text-red-500 hover:text-red-600'}
                    >
                      {student.fullName}
                    </Link>
                  </td>
                  <td>{student.supervisorName ?? 'Не призначено'}</td>
                  <td className={physicalCount === physicalItems.length ? 'text-green-500' : 'text-red-500'}>
                    {physicalCount}/{physicalItems.length}
                  </td>
                  <td className={electronicCount === electronicItems.length ? 'text-green-500' : 'text-red-500'}>
                    {electronicCount}/{electronicItems.length}
                  </td>
                  <td>
                    <StatusBadge admitted={admitted} />
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
        {students.length === 0 && <SectionMessage>У цій групі ще немає студентів.</SectionMessage>}
      </div>

      <div className="mt-16 flex flex-wrap items-center justify-between gap-4">
        <div className="space-y-3">
          <Link
            to={makePath(`/groups/${defenseYear}/${group.id}/material-components`, educationLevel)}
            className="block rounded-full border-2 border-blue-600 px-5 py-2 text-lg font-bold text-blue-600 transition hover:bg-blue-600 hover:text-white"
          >
            Не допущено: Матеріальні
          </Link>
          <Link
            to={makePath(`/groups/${defenseYear}/${group.id}/electronic-components`, educationLevel)}
            className="block rounded-full border-2 border-blue-600 px-5 py-2 text-lg font-bold text-blue-600 transition hover:bg-blue-600 hover:text-white"
          >
            Не допущено: Електронні
          </Link>
        </div>
        <Link
          to={makePath(`/groups/${defenseYear}/${group.id}/results`, educationLevel)}
          className="rounded-full border-2 border-orange-500 px-6 py-3 text-lg font-bold text-orange-600 transition hover:bg-orange-500 hover:text-white"
        >
          Сформувати результати захисту
        </Link>
      </div>
    </article>
  )
}

function ChecklistIcon({ checked }: { checked: boolean }) {
  return checked ? <Check className="mx-auto text-slate-500" size={22} /> : <X className="mx-auto text-red-500" size={22} />
}

function ChecklistTable({
  title,
  students,
  type,
  group,
  educationLevel,
  defenseYear,
}: {
  title: string
  students: GroupStudentResponse[]
  type: 'physical' | 'electronic'
  group: GroupDto
  educationLevel: EducationLevel
  defenseYear: string
}) {
  const items = type === 'physical' ? physicalItems : electronicItems
  const visibleStudents =
    type === 'physical'
      ? students.filter((student) => !isPhysicalChecklistComplete(student.physicalChecklist))
      : students.filter((student) => !isElectronicChecklistComplete(student.electronicChecklist))

  return (
    <div className="overflow-x-auto pb-2">
      <article className="min-w-[1120px] rounded-[22px] bg-white/65 p-8 shadow-sm">
      <div className="flex items-center gap-5">
        <Link to={makePath(`/groups/${defenseYear}/${group.id}`, educationLevel)} className="text-slate-500">
          <ArrowLeft size={38} />
        </Link>
        <h1 className="text-3xl font-bold uppercase text-blue-600">{title}</h1>
      </div>
      <div className="mt-10 overflow-x-auto">
        <table className="w-full min-w-[1040px] table-fixed border-collapse text-center text-slate-500">
          <thead className="border-b border-slate-300 text-xs font-bold">
            <tr>
              <th className="w-12 py-4">ID</th>
              <th className="w-[150px] py-4">ПІБ</th>
              <th className="w-[130px] py-4">Керівник</th>
              {items.map((item) => (
                <th key={item.key} className="break-words px-1 py-4">
                  {item.label}
                </th>
              ))}
              <th className="w-[125px] py-4">Статус</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200 text-xs font-semibold">
            {visibleStudents.map((student, index) => {
              const checklist = type === 'physical' ? student.physicalChecklist : student.electronicChecklist
              const checklistComplete =
                type === 'physical'
                  ? isPhysicalChecklistComplete(student.physicalChecklist)
                  : isElectronicChecklistComplete(student.electronicChecklist)

              return (
                <tr key={student.id} className={checklistComplete ? '' : 'text-red-500'}>
                  <td className="py-5">{index + 1}</td>
                  <td className="break-words px-1">{student.fullName}</td>
                  <td className="text-slate-500">{student.supervisorName ?? 'Не призначено'}</td>
                  {items.map((item) => (
                    <td key={item.key}>
                      <ChecklistIcon
                        checked={
                          type === 'physical'
                            ? Boolean((checklist as PhysicalChecklistDto | null)?.[item.key as keyof PhysicalChecklistDto])
                            : Boolean(
                                (checklist as ElectronicChecklistDto | null)?.[
                                  item.key as keyof ElectronicChecklistDto
                                ],
                              )
                        }
                      />
                    </td>
                  ))}
                  <td>
                    <StatusBadge admitted={checklistComplete} />
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
        {visibleStudents.length === 0 && <SectionMessage>Усі студенти допущені за цим блоком.</SectionMessage>}
      </div>
      </article>
    </div>
  )
}

function AdmissionScreen({
  students,
  group,
  educationLevel,
  defenseYear,
}: {
  students: GroupStudentResponse[]
  group: GroupDto
  educationLevel: EducationLevel
  defenseYear: string
}) {
  const admitted = students.filter(isStudentAdmitted).length
  const rejected = students.length - admitted
  const admittedPercent = students.length > 0 ? Math.round((admitted / students.length) * 100) : 0

  return (
    <div className="overflow-x-auto pb-2">
      <article className="min-w-[1120px] rounded-[22px] bg-white/65 p-10 shadow-sm">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-5">
          <Link to={makePath(`/groups/${defenseYear}/${group.id}`, educationLevel)} className="text-slate-500">
            <ArrowLeft size={38} />
          </Link>
          <h1 className="text-4xl font-bold uppercase text-blue-600">Допуск до захисту {group.name}</h1>
        </div>
        <Link
          to={makePath(`/groups/${defenseYear}/${group.id}`, educationLevel)}
          aria-label="Згорнути групу"
          className="text-slate-500 transition hover:text-blue-600"
        >
          <Minimize2 size={32} />
        </Link>
      </div>
      <div className="mt-14 grid grid-cols-[1fr_360px] gap-10">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[720px] table-fixed border-collapse text-center text-slate-500">
            <thead className="border-b border-slate-300 text-sm font-bold">
              <tr>
                <th className="py-4">ID</th>
                <th className="py-4">ПІБ</th>
                <th className="py-4">Керівник</th>
                <th className="py-4">Матеріальні компоненти</th>
                <th className="py-4">Електронні компоненти</th>
                <th className="py-4">Статус</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 text-sm font-semibold">
              {students.map((student, index) => {
                const studentAdmitted = isStudentAdmitted(student)
                return (
                  <tr key={student.id} className={studentAdmitted ? '' : 'text-red-500'}>
                    <td className="py-5">{index + 1}</td>
                    <td>{student.fullName}</td>
                    <td className="text-slate-500">{student.supervisorName ?? 'Не призначено'}</td>
                    <td className="text-green-500">
                      {countChecked(student.physicalChecklist, physicalItems.map((item) => item.key))}/
                      {physicalItems.length}
                    </td>
                    <td className={studentAdmitted ? 'text-green-500' : 'text-red-500'}>
                      {countChecked(student.electronicChecklist, electronicItems.map((item) => item.key))}/
                      {electronicItems.length}
                    </td>
                    <td>
                      <StatusBadge admitted={studentAdmitted} />
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
        <div className="self-center rounded-[18px] bg-white p-7 shadow-sm">
          <div className="grid grid-cols-3 gap-8 text-center">
            <div>
              <p className="text-5xl font-bold text-slate-500">{students.length}</p>
              <p className="mt-3 text-xl font-bold uppercase">Усього</p>
            </div>
            <div>
              <p className="text-5xl font-bold text-green-500">{admitted}</p>
              <p className="mt-3 text-xl font-bold uppercase text-green-500">Допущено</p>
            </div>
            <div>
              <p className="text-5xl font-bold text-red-500">{rejected}</p>
              <p className="mt-3 text-xl font-bold uppercase text-red-500">Не допущено</p>
            </div>
          </div>
          <div className="mt-12 flex items-center gap-3 text-lg font-bold">
            <span className="text-green-500">{admittedPercent}%</span>
            <div className="h-12 flex-1 overflow-hidden rounded-full bg-red-500">
              <div className="h-full rounded-full bg-green-500" style={{ width: `${admittedPercent}%` }} />
            </div>
            <span className="text-red-500">{100 - admittedPercent}%</span>
          </div>
        </div>
      </div>
      </article>
    </div>
  )
}

function ResultsScreen({
  group,
  educationLevel,
  defenseYear,
  secretaryEmail,
}: {
  group: GroupDto
  educationLevel: EducationLevel
  defenseYear: string
  secretaryEmail: string
}) {
  const statisticsQuery = useQuery(groupStatisticsQuery(group.id, secretaryEmail))

  return (
    <div className="overflow-x-auto pb-2">
      <article className="min-w-[1120px] rounded-[22px] bg-white/65 p-9 shadow-sm">
      <div className="flex items-center gap-5">
        <Link to={makePath(`/groups/${defenseYear}/${group.id}`, educationLevel)} className="text-slate-500">
          <ArrowLeft size={38} />
        </Link>
        <h1 className="text-4xl font-bold uppercase text-blue-600">Результати захисту {group.name}</h1>
      </div>
      <div className="mt-10">
        {statisticsQuery.isLoading && <SectionMessage>Завантажуємо статистику...</SectionMessage>}
        {statisticsQuery.error && <ErrorMessage error={statisticsQuery.error} />}
        {statisticsQuery.data && (
          <div className="grid grid-cols-[1.3fr_0.7fr] gap-8">
            <div className="space-y-6">
              {statisticsQuery.data.sections.map((section) => (
                <div key={section.key} className="rounded-[16px] border border-slate-300 bg-white p-6">
                  <h2 className="text-sm font-bold uppercase text-slate-500">{section.title}</h2>
                  <div className="mt-6 space-y-5">
                    {section.items.map((item, index) => (
                      <div key={item.key} className="grid grid-cols-[1fr_280px] items-center gap-6">
                        <span className="text-lg font-bold text-slate-600">{item.label}</span>
                        <div className="flex items-center gap-3">
                          <div className="h-2 flex-1 bg-slate-200">
                            <div
                              className={[
                                'h-full',
                                index % 4 === 0
                                  ? 'bg-green-500'
                                  : index % 4 === 1
                                    ? 'bg-blue-600'
                                    : index % 4 === 2
                                      ? 'bg-orange-500'
                                      : 'bg-red-500',
                              ].join(' ')}
                              style={{ width: `${Number(item.percentage)}%` }}
                            />
                          </div>
                          <span className="grid w-28 grid-cols-[32px_1px_1fr] items-center gap-2 text-right font-bold text-slate-500">
                            <span>{item.count}</span>
                            <span className="h-5 w-px bg-slate-300" aria-hidden="true" />
                            <span>{Number(item.percentage).toFixed(1)}%</span>
                          </span>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
            <div className="rounded-[16px] bg-white p-8">
              <h2 className="text-sm font-bold uppercase text-slate-500">Підсумок</h2>
              <p className="mt-8 text-6xl font-bold text-blue-600">{statisticsQuery.data.totalStudents}</p>
              <p className="mt-2 text-xl font-bold text-slate-500">студентів у статистиці</p>
            </div>
          </div>
        )}
      </div>
      </article>
    </div>
  )
}

interface StudentFormState {
  lastName: string
  firstName: string
  middleName: string
  topic: string
  supervisorId: string
  practiceBase: string
  reviewerId: string
  physical: PhysicalChecklistDto
  electronic: ElectronicChecklistDto
  defenceDate: string
  plagiarismPercent: string
  uniquePercent: string
  supervisorScore: string
  reviewerScore: string
  commissionScore: string
  ectsGrade: string
  nationalGrade: string
  hasDiplomaWithHonors: boolean
  characteristics: CharacteristicsDto
}

function studentFormFromDetails(details: StudentDetailsResponse): StudentFormState {
  return {
    lastName: details.name.lastName,
    firstName: details.name.firstName,
    middleName: details.name.middleName,
    topic: details.qualificationWork?.topic ?? '',
    supervisorId: asString(details.qualificationWork?.supervisorId ?? undefined),
    practiceBase: details.qualificationWork?.practiceBase ?? '',
    reviewerId: asString(details.qualificationWork?.reviewerId ?? undefined),
    physical: details.physicalChecklist ?? emptyPhysicalChecklist,
    electronic: details.electronicChecklist ?? emptyElectronicChecklist,
    defenceDate: details.defenceInfo?.defenceDate ?? '',
    plagiarismPercent: asString(details.defenceResults?.plagiarismPercent ?? undefined),
    uniquePercent: asString(details.defenceResults?.uniquePercent ?? undefined),
    supervisorScore: asString(details.defenceResults?.supervisorScore ?? undefined),
    reviewerScore: asString(details.defenceResults?.reviewerScore ?? undefined),
    commissionScore: asString(details.defenceResults?.commissionScore ?? undefined),
    ectsGrade: details.defenceResults?.ectsGrade ?? '',
    nationalGrade: details.defenceResults?.nationalGrade ?? '',
    hasDiplomaWithHonors: details.defenceResults?.hasDiplomaWithHonors ?? false,
    characteristics: details.characteristics ?? emptyCharacteristics,
  }
}

function hasChanged(a: unknown, b: unknown) {
  return JSON.stringify(a) !== JSON.stringify(b)
}

function InputField({
  label,
  value,
  onChange,
  disabled,
  type = 'text',
}: {
  label: string
  value: string
  onChange: (value: string) => void
  disabled: boolean
  type?: string
}) {
  return (
    <label className="grid grid-cols-[170px_1fr] items-center gap-5 text-sm font-bold text-slate-600">
      <span>{label}</span>
      <input
        type={type}
        value={value}
        disabled={disabled}
        onChange={(event) => onChange(event.target.value)}
        className="h-9 rounded-lg border border-slate-300 bg-transparent px-3 outline-none transition focus:border-blue-500 disabled:text-slate-500"
      />
    </label>
  )
}

function StudentDetailsPanel({
  studentId,
  group,
  years,
  students,
  educationLevel,
  defenseYear,
  secretaryEmail,
  onDeleteStudent,
}: {
  studentId: EntityId
  group: GroupDto
  years: AcademicYearOverviewResponse[]
  students: GroupStudentResponse[]
  educationLevel: EducationLevel
  defenseYear: string
  secretaryEmail: string
  onDeleteStudent: (student: StudentDetailsResponse) => void
}) {
  const queryClient = useQueryClient()
  const { showError, showSuccess } = useToast()
  const detailsQuery = useQuery(studentDetailsQuery(studentId, secretaryEmail))
  const optionsQuery = useQuery(qualificationWorkOptionsQuery(studentId, secretaryEmail))
  const [isEditing, setIsEditing] = useState(false)
  const [draftForm, setDraftForm] = useState<StudentFormState | null>(null)
  const saveMutation = useMutation({
    mutationFn: async ({ details, current }: { details: StudentDetailsResponse; current: StudentFormState }) => {
      const original = studentFormFromDetails(details)
      const requests: Array<Promise<unknown>> = []

      if (
        hasChanged(
          { lastName: original.lastName, firstName: original.firstName, middleName: original.middleName },
          { lastName: current.lastName, firstName: current.firstName, middleName: current.middleName },
        )
      ) {
        requests.push(
          updateStudentName(studentId, {
            secretaryEmail,
            lastName: current.lastName,
            firstName: current.firstName,
            middleName: current.middleName,
          }),
        )
      }

      if (
        hasChanged(
          {
            topic: original.topic,
            supervisorId: original.supervisorId,
            practiceBase: original.practiceBase,
            reviewerId: original.reviewerId,
          },
          {
            topic: current.topic,
            supervisorId: current.supervisorId,
            practiceBase: current.practiceBase,
            reviewerId: current.reviewerId,
          },
        )
      ) {
        requests.push(
          updateStudentQualificationWork(studentId, {
            secretaryEmail,
            topic: current.topic,
            supervisorId: current.supervisorId || null,
            practiceBase: current.practiceBase,
            reviewerId: current.reviewerId || null,
          }),
        )
      }

      if (hasChanged(original.physical, current.physical)) {
        requests.push(updatePhysicalChecklist(studentId, { secretaryEmail, ...current.physical }))
      }

      if (hasChanged(original.electronic, current.electronic)) {
        requests.push(updateElectronicChecklist(studentId, { secretaryEmail, ...current.electronic }))
      }

      if (hasChanged(original.defenceDate, current.defenceDate)) {
        requests.push(updateStudentDefence(studentId, { secretaryEmail, defenceDate: current.defenceDate || null }))
      }

      if (
        hasChanged(
          {
            plagiarismPercent: original.plagiarismPercent,
            uniquePercent: original.uniquePercent,
            supervisorScore: original.supervisorScore,
            reviewerScore: original.reviewerScore,
            commissionScore: original.commissionScore,
            ectsGrade: original.ectsGrade,
            nationalGrade: original.nationalGrade,
            hasDiplomaWithHonors: original.hasDiplomaWithHonors,
          },
          {
            plagiarismPercent: current.plagiarismPercent,
            uniquePercent: current.uniquePercent,
            supervisorScore: current.supervisorScore,
            reviewerScore: current.reviewerScore,
            commissionScore: current.commissionScore,
            ectsGrade: current.ectsGrade,
            nationalGrade: current.nationalGrade,
            hasDiplomaWithHonors: current.hasDiplomaWithHonors,
          },
        )
      ) {
        requests.push(
          updateDefenceResults(studentId, {
            secretaryEmail,
            plagiarismPercent: current.plagiarismPercent,
            uniquePercent: current.uniquePercent,
            supervisorScore: current.supervisorScore,
            reviewerScore: current.reviewerScore,
            commissionScore: current.commissionScore,
            ectsGrade: current.ectsGrade,
            nationalGrade: current.nationalGrade,
            hasDiplomaWithHonors: current.hasDiplomaWithHonors,
          }),
        )
      }

      if (hasChanged(original.characteristics, current.characteristics)) {
        requests.push(updateQualificationWorkCharacteristics(studentId, { secretaryEmail, ...current.characteristics }))
      }

      await Promise.all(requests)
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: groupsQueryKeys.studentDetails(studentId, secretaryEmail) }),
        queryClient.invalidateQueries({ queryKey: groupsQueryKeys.students(group.id, secretaryEmail) }),
      ])
      setIsEditing(false)
      setDraftForm(null)
      showSuccess()
    },
    onError: (error) => showError(getApiErrorMessages(error)),
  })

  if (detailsQuery.error) {
    return <ErrorMessage error={detailsQuery.error} />
  }

  const serverForm = detailsQuery.data ? studentFormFromDetails(detailsQuery.data) : null
  const form = isEditing ? draftForm : serverForm

  if (detailsQuery.isLoading || !detailsQuery.data || !form) {
    return <SectionMessage>Завантажуємо студента...</SectionMessage>
  }

  const details = detailsQuery.data
  const selectedGroupStudentsPath = makePath(`/groups/${defenseYear}/${group.id}`, educationLevel)
  const updateForm = (patch: Partial<StudentFormState>) =>
    setDraftForm((current) => (current ? { ...current, ...patch } : { ...form, ...patch }))
  const togglePhysical = (key: keyof PhysicalChecklistDto) =>
    updateForm({ physical: { ...form.physical, [key]: !form.physical[key] } })
  const toggleElectronic = (key: keyof ElectronicChecklistDto) =>
    updateForm({ electronic: { ...form.electronic, [key]: !form.electronic[key] } })
  const toggleCharacteristic = (key: keyof CharacteristicsDto) =>
    updateForm({ characteristics: { ...form.characteristics, [key]: !form.characteristics[key] } })
  const cancelEdit = () => {
    setIsEditing(false)
    setDraftForm(null)
  }

  return (
    <div className="grid grid-cols-[360px_1fr] gap-8">
      <div className="space-y-8">
        <YearTabs years={years} activeDefenseYear={defenseYear} educationLevel={educationLevel} />
        <aside className="rounded-[22px] bg-white/65 p-8 shadow-sm">
          <h2 className="text-xl font-bold uppercase text-slate-500">Групи</h2>
          <Link
            to={makePath(`/groups/${defenseYear}/${group.id}`, educationLevel)}
            className="mt-8 block rounded-2xl border border-blue-600 px-5 py-4 text-2xl font-bold text-blue-600"
          >
            {group.name}
          </Link>
          <div className="mt-6 space-y-3">
            {students.map((student) => (
              <Link
                key={student.id}
                to={makePath(`/groups/${defenseYear}/${group.id}/students/${student.id}`, educationLevel)}
                className={[
                  'block rounded-2xl px-5 py-3 text-xl font-bold transition',
                  asString(student.id) === asString(studentId)
                    ? 'bg-blue-600 text-white'
                    : 'text-slate-500 hover:bg-white',
                ].join(' ')}
              >
                {student.fullName}
              </Link>
            ))}
          </div>
        </aside>
      </div>
      <article className="rounded-[22px] bg-white/65 p-9 shadow-sm">
        <div className="flex items-start justify-between gap-6">
          <div className="flex items-start gap-5">
            <Link to={selectedGroupStudentsPath} className="mt-2 text-slate-500">
              <ArrowLeft size={38} />
            </Link>
            <h1 className="text-4xl font-bold text-blue-600">{details.fullName}</h1>
          </div>
          <div className="flex gap-3">
            <button
              type="button"
              onClick={() => onDeleteStudent(details)}
              className="h-11 rounded-full border-2 border-red-500 px-8 font-bold text-red-500 transition hover:bg-red-500 hover:text-white"
            >
              Видалити
            </button>
            <button
              type="button"
              onClick={
                isEditing
                  ? cancelEdit
                  : () => {
                      setDraftForm(form)
                      setIsEditing(true)
                    }
              }
              className="h-11 rounded-full border-2 border-blue-600 px-8 font-bold text-blue-600 transition hover:bg-blue-600 hover:text-white"
            >
              {isEditing ? 'Скасувати' : 'Змінити'}
            </button>
          </div>
        </div>

        {!isEditing ? (
          <StudentCollapsedSections details={details} form={form} />
        ) : (
          <div className="mt-8 space-y-9">
          <section className="space-y-3">
            <h2 className="text-sm font-bold uppercase text-slate-500">Загальна інформація</h2>
            <InputField label="Прізвище" value={form.lastName} disabled={!isEditing} onChange={(lastName) => updateForm({ lastName })} />
            <InputField label="Ім’я" value={form.firstName} disabled={!isEditing} onChange={(firstName) => updateForm({ firstName })} />
            <InputField label="По-батькові" value={form.middleName} disabled={!isEditing} onChange={(middleName) => updateForm({ middleName })} />
            <InputField label="Тема роботи" value={form.topic} disabled={!isEditing} onChange={(topic) => updateForm({ topic })} />
            <label className="grid grid-cols-[170px_1fr] items-center gap-5 text-sm font-bold text-slate-600">
              <span>Керівник роботи</span>
              <select
                value={form.supervisorId}
                disabled={!isEditing}
                onChange={(event) => updateForm({ supervisorId: event.target.value })}
                className="h-9 rounded-lg border border-slate-300 bg-transparent px-3 outline-none focus:border-blue-500 disabled:text-slate-500"
              >
                <option value="">Не призначено</option>
                {optionsQuery.data?.supervisors.map((teacher) => (
                  <option key={teacher.id} value={teacher.id}>
                    {teacher.shortName}
                  </option>
                ))}
              </select>
            </label>
            <InputField label="База практики" value={form.practiceBase} disabled={!isEditing} onChange={(practiceBase) => updateForm({ practiceBase })} />
            <label className="grid grid-cols-[170px_1fr] items-center gap-5 text-sm font-bold text-slate-600">
              <span>Рецензент роботи</span>
              <select
                value={form.reviewerId}
                disabled={!isEditing}
                onChange={(event) => updateForm({ reviewerId: event.target.value })}
                className="h-9 rounded-lg border border-slate-300 bg-transparent px-3 outline-none focus:border-blue-500 disabled:text-slate-500"
              >
                <option value="">Не призначено</option>
                {optionsQuery.data?.reviewers.map((teacher) => (
                  <option key={teacher.id} value={teacher.id}>
                    {teacher.shortName}
                  </option>
                ))}
              </select>
            </label>
          </section>

          <section>
            <h2 className="text-sm font-bold uppercase text-slate-500">Інформація про проходження дипломування</h2>
            <div className="mt-4 grid grid-cols-2 gap-10">
              <ChecklistEditor
                title="Матеріальні компоненти"
                checkedCount={countChecked(form.physical, physicalItems.map((item) => item.key))}
                total={physicalItems.length}
                disabled={!isEditing}
                items={physicalItems}
                values={form.physical}
                onToggle={togglePhysical}
              />
              <ChecklistEditor
                title="Електронні компоненти"
                checkedCount={countChecked(form.electronic, electronicItems.map((item) => item.key))}
                total={electronicItems.length}
                disabled={!isEditing}
                items={electronicItems}
                values={form.electronic}
                onToggle={toggleElectronic}
              />
            </div>
          </section>

          <p className="font-bold text-slate-500">
            Дані в блоці <span className="text-orange-600">“Інформація про захист”</span> та{' '}
            <span className="text-orange-600">“Результати захисту”</span> підтягнуться після завантаження файлу з
            результатами захисту групи
          </p>

          <section className="space-y-3">
            <h2 className="text-sm font-bold uppercase text-slate-500">Інформація про захист</h2>
            <InputField label="Дата захисту" value={form.defenceDate} disabled={!isEditing} type="date" onChange={(defenceDate) => updateForm({ defenceDate })} />
          </section>

          <section className="space-y-3">
            <h2 className="text-sm font-bold uppercase text-slate-500">Результати захисту</h2>
            <InputField label="Відсоток запозичення" value={form.plagiarismPercent} disabled={!isEditing} onChange={(plagiarismPercent) => updateForm({ plagiarismPercent })} />
            <InputField label="Унікальність роботи" value={form.uniquePercent} disabled={!isEditing} onChange={(uniquePercent) => updateForm({ uniquePercent })} />
            <InputField label="Оцінка керівника" value={form.supervisorScore} disabled={!isEditing} onChange={(supervisorScore) => updateForm({ supervisorScore })} />
            <InputField label="Оцінка рецензента" value={form.reviewerScore} disabled={!isEditing} onChange={(reviewerScore) => updateForm({ reviewerScore })} />
            <InputField label="Оцінка ДЕК" value={form.commissionScore} disabled={!isEditing} onChange={(commissionScore) => updateForm({ commissionScore })} />
            <InputField label="Оцінка ECTS" value={form.ectsGrade} disabled={!isEditing} onChange={(ectsGrade) => updateForm({ ectsGrade })} />
            <InputField label="Національна шкала" value={form.nationalGrade} disabled={!isEditing} onChange={(nationalGrade) => updateForm({ nationalGrade })} />
            <CheckboxLine
              label="Диплом з відзнакою"
              checked={form.hasDiplomaWithHonors}
              disabled={!isEditing}
              onChange={() => updateForm({ hasDiplomaWithHonors: !form.hasDiplomaWithHonors })}
            />
          </section>

          <section className="space-y-3">
            <h2 className="text-sm font-bold uppercase text-slate-500">Характеристика кваліфікаційної роботи</h2>
            {characteristicItems.map((item) => (
              <CheckboxLine
                key={item.key}
                label={item.label}
                checked={form.characteristics[item.key]}
                disabled={!isEditing}
                onChange={() => toggleCharacteristic(item.key)}
              />
            ))}
          </section>
          </div>
        )}

        {isEditing && (
          <div className="mt-10 flex justify-end gap-3">
            <button
              type="button"
              onClick={cancelEdit}
              className="h-11 rounded-full border-2 border-blue-600 px-8 font-bold text-blue-600 transition hover:bg-blue-600 hover:text-white"
            >
              Скасувати
            </button>
            <button
              type="button"
              disabled={saveMutation.isPending}
              onClick={() => saveMutation.mutate({ details, current: form })}
              className="h-11 rounded-full border-2 border-green-500 px-8 font-bold text-green-600 transition hover:bg-green-500 hover:text-white disabled:opacity-60 disabled:hover:bg-transparent disabled:hover:text-green-600"
            >
              Зберегти зміни
            </button>
          </div>
        )}
      </article>
    </div>
  )
}

function CheckboxLine({
  label,
  checked,
  disabled,
  onChange,
}: {
  label: string
  checked: boolean
  disabled: boolean
  onChange: () => void
}) {
  return (
    <label className="grid grid-cols-[1fr_32px] items-center gap-4 text-sm font-bold text-slate-600">
      <span>{label}</span>
      <input type="checkbox" checked={checked} disabled={disabled} onChange={onChange} className="size-5 accent-orange-500" />
    </label>
  )
}

function StudentCollapsedSections({ details, form }: { details: StudentDetailsResponse; form: StudentFormState }) {
  const [openSections, setOpenSections] = useState<Record<string, boolean>>({})
  const sections = [
    {
      key: 'general',
      title: 'Загальна інформація',
      content: (
        <div className="grid gap-3">
          <ReadOnlyRow label="Прізвище" value={form.lastName} />
          <ReadOnlyRow label="Ім’я" value={form.firstName} />
          <ReadOnlyRow label="По-батькові" value={form.middleName} />
          <ReadOnlyRow label="Тема роботи" value={form.topic} />
          <ReadOnlyRow label="Керівник роботи" value={details.qualificationWork?.supervisorName} />
          <ReadOnlyRow label="База практики" value={form.practiceBase} />
          <ReadOnlyRow label="Рецензент роботи" value={details.qualificationWork?.reviewerName} />
        </div>
      ),
    },
    {
      key: 'process',
      title: 'Інформація про проходження дипломування',
      content: (
        <div className="grid gap-8 lg:grid-cols-2">
          <ChecklistEditor
            title="Матеріальні компоненти"
            checkedCount={countChecked(form.physical, physicalItems.map((item) => item.key))}
            total={physicalItems.length}
            disabled
            items={physicalItems}
            values={form.physical}
            onToggle={() => undefined}
          />
          <ChecklistEditor
            title="Електронні компоненти"
            checkedCount={countChecked(form.electronic, electronicItems.map((item) => item.key))}
            total={electronicItems.length}
            disabled
            items={electronicItems}
            values={form.electronic}
            onToggle={() => undefined}
          />
        </div>
      ),
    },
    {
      key: 'defence',
      title: 'Інформація про захист',
      content: <ReadOnlyRow label="Дата захисту" value={formatDateOnly(form.defenceDate)} />,
    },
    {
      key: 'results',
      title: 'Результати захисту',
      content: (
        <div className="grid gap-3">
          <ReadOnlyRow label="Відсоток запозичення" value={form.plagiarismPercent} />
          <ReadOnlyRow label="Унікальність роботи" value={form.uniquePercent} />
          <ReadOnlyRow label="Оцінка керівника" value={form.supervisorScore} />
          <ReadOnlyRow label="Оцінка рецензента" value={form.reviewerScore} />
          <ReadOnlyRow label="Оцінка ДЕК" value={form.commissionScore} />
          <ReadOnlyRow label="Оцінка ECTS" value={form.ectsGrade} />
          <ReadOnlyRow label="Національна шкала" value={form.nationalGrade} />
          <ReadOnlyBoolean label="Диплом з відзнакою" checked={form.hasDiplomaWithHonors} />
        </div>
      ),
    },
    {
      key: 'qualification-characteristics',
      title: 'Характеристика кваліфікаційної роботи',
      content: <ReadOnlyChecklist items={characteristicItems.slice(0, 4)} values={form.characteristics} />,
    },
    {
      key: 'complex',
      title: 'Комплексне дипломне проектування',
      content: <ReadOnlyChecklist items={characteristicItems.slice(4, 8)} values={form.characteristics} />,
    },
    {
      key: 'recommended',
      title: 'Рекомендовано та захищено',
      content: <ReadOnlyChecklist items={characteristicItems.slice(8)} values={form.characteristics} />,
    },
  ]

  const toggleSection = (key: string) => {
    setOpenSections((current) => ({ ...current, [key]: !current[key] }))
  }

  return (
    <div className="mt-8 max-w-[700px] space-y-4">
      {sections.map((section) => {
        const isOpen = Boolean(openSections[section.key])

        return (
          <section key={section.key} className="space-y-3">
            <button
              type="button"
              onClick={() => toggleSection(section.key)}
              aria-expanded={isOpen}
              className="flex w-full items-center gap-3 text-left text-sm font-bold uppercase text-slate-500 transition hover:text-blue-600"
            >
              <span>{section.title}</span>
              {isOpen ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
            </button>
            {isOpen && <div className="pl-5">{section.content}</div>}
          </section>
        )
      })}
    </div>
  )
}

function ReadOnlyRow({ label, value }: { label: string; value: string | number | null | undefined }) {
  return (
    <div className="grid grid-cols-[170px_1fr] items-start gap-5 text-sm font-bold text-slate-600">
      <span>{label}</span>
      <span className="min-h-8 rounded-lg border border-slate-200 px-3 py-1.5 text-slate-500">{displayValue(value)}</span>
    </div>
  )
}

function ReadOnlyBoolean({ label, checked }: { label: string; checked: boolean }) {
  return <CheckboxLine label={label} checked={checked} disabled onChange={() => undefined} />
}

function ReadOnlyChecklist<T extends object>({
  items,
  values,
}: {
  items: Array<{ key: keyof T; label: string }>
  values: T
}) {
  return (
    <div className="space-y-3">
      {items.map((item) => (
        <CheckboxLine
          key={String(item.key)}
          label={item.label}
          checked={Boolean(values[item.key])}
          disabled
          onChange={() => undefined}
        />
      ))}
    </div>
  )
}

function displayValue(value: string | number | null | undefined) {
  if (value === null || value === undefined || value === '') {
    return '—'
  }

  return String(value)
}

function formatDateOnly(value: string) {
  if (!value) {
    return ''
  }

  const [year, month, day] = value.split('-')
  return day && month && year ? `${day}.${month}.${year}` : value
}

function ChecklistEditor<T extends object>({
  title,
  checkedCount,
  total,
  disabled,
  items,
  values,
  onToggle,
}: {
  title: string
  checkedCount: number
  total: number
  disabled: boolean
  items: Array<{ key: keyof T; label: string }>
  values: T
  onToggle: (key: keyof T) => void
}) {
  return (
    <div>
      <h3 className="mb-4 flex items-center justify-between text-sm font-bold text-slate-600">
        <span>{title}</span>
        <span className={checkedCount === total ? 'text-green-500' : 'text-red-500'}>
          {checkedCount}/{total}
        </span>
      </h3>
      <div className="space-y-3">
        {items.map((item) => (
          <CheckboxLine
            key={String(item.key)}
            label={item.label}
            checked={Boolean(values[item.key])}
            disabled={disabled}
            onChange={() => onToggle(item.key)}
          />
        ))}
      </div>
    </div>
  )
}

function GroupDialog({
  mode,
  educationLevel,
  initialGroup,
  initialYear,
  secretaryEmail,
  onClose,
  onSuccess,
}: {
  mode: 'create' | 'edit'
  educationLevel: EducationLevel
  initialGroup?: GroupDto
  initialYear?: string
  secretaryEmail: string
  onClose: () => void
  onSuccess: (defenseYear?: string, groupId?: EntityId) => void
}) {
  const { showError, showSuccess } = useToast()
  const [name, setName] = useState(initialGroup?.name ?? '')
  const [year, setYear] = useState(initialYear ?? currentDefenseYears()[0])
  const [driveUrl, setDriveUrl] = useState('')
  const [file, setFile] = useState<File | null>(null)
  const handleFileDrop = (event: DragEvent<HTMLLabelElement>) => {
    event.preventDefault()
    setFile(event.dataTransfer.files?.[0] ?? null)
  }
  const mutation = useMutation({
    mutationFn: async (): Promise<CreateGroupResponse | UpdateGroupResponse> => {
      if (mode === 'create') {
        return createGroup({
          secretaryEmail,
          name,
          year,
          educationLevel,
          studentsFile: file,
          googleDriveUrl: driveUrl.trim() || null,
        })
      }

      return updateGroup(initialGroup?.id ?? '', {
        secretaryEmail,
        name,
        year,
        educationLevel,
      })
    },
    onSuccess: (response) => {
      showSuccess()
      if ('groupId' in response) {
        onSuccess(response.defenseYear, response.groupId)
      } else {
        onSuccess(response.defenseYear, response.id)
      }
    },
    onError: (apiError) => showError(getApiErrorMessages(apiError)),
  })
  const submit = () => {
    if (!name.trim()) {
      const message = 'Вкажіть назву групи.'
      showError(message)
      return
    }

    if (mode === 'create' && !file && !driveUrl.trim()) {
      const message = 'Завантажте файл зі студентами або вкажіть посилання Google Drive.'
      showError(message)
      return
    }

    mutation.mutate()
  }

  return (
    <div className="fixed inset-0 z-40 overflow-y-auto bg-[#dcecff]/80 px-6 py-16 backdrop-blur-sm">
      <section className="mx-auto min-h-[620px] max-w-[1280px] rounded-[28px] bg-white/80 p-10 shadow-xl">
        <div className="flex items-start justify-between">
          <h2 className="text-4xl font-bold uppercase text-blue-600">
            {mode === 'create' ? 'Створення групи' : 'Зміна групи'}
          </h2>
          <button type="button" onClick={onClose} aria-label="Закрити" className="text-red-500">
            <X size={42} />
          </button>
        </div>
        <div className="mt-10 max-w-[1120px] space-y-8">
          <h3 className="text-sm font-bold uppercase text-slate-500">Загальна інформація</h3>
          <label className="grid max-w-[580px] grid-cols-[220px_1fr] items-center gap-6 text-lg font-bold">
            <span>Назва групи</span>
            <input value={name} onChange={(event) => setName(event.target.value)} className="h-12 rounded-xl border border-slate-300 bg-transparent px-4 outline-none focus:border-blue-500" />
          </label>
          <label className="grid max-w-[580px] grid-cols-[220px_1fr] items-center gap-6 text-lg font-bold">
            <span>Рік захисту</span>
            <select value={year} onChange={(event) => setYear(event.target.value)} className="h-12 rounded-xl border border-slate-300 bg-transparent px-4 outline-none focus:border-blue-500">
              {currentDefenseYears().map((item) => (
                <option key={item} value={item}>
                  {item}
                </option>
              ))}
            </select>
          </label>
          <label className="grid max-w-[580px] grid-cols-[220px_1fr] items-center gap-6 text-lg font-bold">
            <span>ОКР</span>
            <input value={educationOptions.find((item) => item.value === educationLevel)?.label ?? educationLevel} disabled className="h-12 rounded-xl border border-slate-300 bg-transparent px-4 text-slate-500" />
          </label>

          {mode === 'create' && (
            <div>
              <h3 className="text-sm font-bold uppercase text-slate-500">Студенти</h3>
              <p className="mt-2 text-xl font-bold text-slate-500">
                Завантажте файл з переліком студентів групи, або залиште посилання на Google Drive
              </p>
              <div className="mt-8 grid grid-cols-[1fr_80px_1fr] items-center gap-8">
                <label
                  onDragOver={(event) => event.preventDefault()}
                  onDrop={handleFileDrop}
                  className="grid min-h-56 cursor-pointer place-items-center rounded-xl border-2 border-dashed border-blue-500 text-center text-xl font-bold text-slate-600"
                >
                  <span>
                    <Upload className="mx-auto mb-6" size={58} />
                    {file ? file.name : 'Перетягніть файл сюди або натисніть'}
                  </span>
                  <input type="file" className="hidden" onChange={(event) => setFile(event.target.files?.[0] ?? null)} />
                </label>
                <span className="text-center text-xl font-bold text-slate-500">або</span>
                <textarea
                  value={driveUrl}
                  onChange={(event) => setDriveUrl(event.target.value)}
                  placeholder="Приклад: посилання Google Drive"
                  className="min-h-56 rounded-xl border border-slate-300 bg-transparent p-5 text-xl font-bold outline-none placeholder:text-slate-400 focus:border-blue-500"
                />
              </div>
            </div>
          )}
        </div>
        <div className="mt-12 flex justify-end gap-3">
          <button type="button" onClick={onClose} className="h-12 rounded-full border-2 border-blue-600 px-8 text-lg font-bold text-blue-600 transition hover:bg-blue-600 hover:text-white">
            Скасувати
          </button>
          <button
            type="button"
            onClick={submit}
            disabled={mutation.isPending}
            className="h-12 rounded-full border-2 border-green-500 px-8 text-lg font-bold text-green-600 transition hover:bg-green-500 hover:text-white disabled:opacity-50 disabled:hover:bg-transparent disabled:hover:text-green-600"
          >
            {mode === 'create' ? 'Створити' : 'Зберегти'}
          </button>
        </div>
      </section>
    </div>
  )
}

function AddStudentDialog({
  secretaryEmail,
  groupId,
  onClose,
  onSuccess,
}: {
  secretaryEmail: string
  groupId: EntityId
  onClose: () => void
  onSuccess: () => void
}) {
  const { showError, showSuccess } = useToast()
  const [lastName, setLastName] = useState('')
  const [firstName, setFirstName] = useState('')
  const [middleName, setMiddleName] = useState('')
  const mutation = useMutation({
    mutationFn: () => addStudent(groupId, { secretaryEmail, lastName, firstName, middleName }),
    onSuccess: () => {
      showSuccess()
      onSuccess()
    },
    onError: (apiError) => showError(getApiErrorMessages(apiError)),
  })
  const submit = () => {
    if (!lastName.trim() || !firstName.trim() || !middleName.trim()) {
      const message = 'Заповніть ПІБ студента.'
      showError(message)
      return
    }
    mutation.mutate()
  }

  return (
    <div className="fixed inset-0 z-40 overflow-y-auto bg-[#dcecff]/80 px-6 py-16 backdrop-blur-sm">
      <section className="mx-auto min-h-[520px] max-w-[1280px] rounded-[28px] bg-white/80 p-10 shadow-xl">
        <div className="flex items-start justify-between">
          <h2 className="text-4xl font-bold uppercase text-blue-600">Додати студента</h2>
          <button type="button" onClick={onClose} aria-label="Закрити" className="text-red-500">
            <X size={42} />
          </button>
        </div>
        <div className="mt-10 max-w-[820px] space-y-7">
          <h3 className="text-sm font-bold uppercase text-slate-500">Загальна інформація</h3>
          <InputField label="Прізвище" value={lastName} disabled={false} onChange={setLastName} />
          <InputField label="Ім’я" value={firstName} disabled={false} onChange={setFirstName} />
          <InputField label="По-батькові" value={middleName} disabled={false} onChange={setMiddleName} />
        </div>
        <div className="mt-16 flex justify-end gap-3">
          <button type="button" onClick={onClose} className="h-12 rounded-full border-2 border-blue-600 px-8 text-lg font-bold text-blue-600 transition hover:bg-blue-600 hover:text-white">
            Скасувати
          </button>
          <button type="button" onClick={submit} disabled={mutation.isPending} className="h-12 rounded-full border-2 border-green-500 px-8 text-lg font-bold text-green-600 transition hover:bg-green-500 hover:text-white disabled:opacity-50 disabled:hover:bg-transparent disabled:hover:text-green-600">
            Додати
          </button>
        </div>
      </section>
    </div>
  )
}

interface CommissionFormState {
  orderNumber: string
  commissionHeadId: string
  firstMemberTeacherId: string
  secondMemberTeacherId: string
  thirdMemberTeacherId: string
  secretaryId: string
  startDate: string
  endDate: string
}

const emptyCommissionForm: CommissionFormState = {
  orderNumber: '',
  commissionHeadId: '',
  firstMemberTeacherId: '',
  secondMemberTeacherId: '',
  thirdMemberTeacherId: '',
  secretaryId: '',
  startDate: '',
  endDate: '',
}

function makeDefaultCommissionForm(defenseYear: string): CommissionFormState {
  return {
    ...emptyCommissionForm,
    startDate: `${defenseYear}-01-01`,
    endDate: `${defenseYear}-12-31`,
  }
}

function commissionFormFromResponse(commission?: DiplomaExaminationCommissionResponse): CommissionFormState {
  if (!commission) {
    return emptyCommissionForm
  }

  return {
    orderNumber: commission.orderNumber,
    commissionHeadId: asString(commission.head.id),
    firstMemberTeacherId: asString(commission.members[0]?.teacherId),
    secondMemberTeacherId: asString(commission.members[1]?.teacherId),
    thirdMemberTeacherId: asString(commission.members[2]?.teacherId),
    secretaryId: asString(commission.secretary.id),
    startDate: commission.startDate,
    endDate: commission.endDate,
  }
}

function CommissionTextField({
  label,
  value,
  onChange,
  placeholder,
  type = 'text',
}: {
  label: string
  value: string
  onChange: (value: string) => void
  placeholder?: string
  type?: string
}) {
  return (
    <label className="grid max-w-[560px] grid-cols-[170px_1fr] items-center gap-6 text-lg font-bold text-slate-700">
      <span>{label}</span>
      <input
        type={type}
        value={value}
        placeholder={placeholder}
        onChange={(event) => onChange(event.target.value)}
        className="h-12 rounded-xl border border-slate-300 bg-transparent px-4 outline-none placeholder:text-slate-400 focus:border-blue-500"
      />
    </label>
  )
}

function CommissionSelectField({
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

function AddCommissionHeadDialog({
  secretaryEmail,
  onClose,
  onSuccess,
}: {
  secretaryEmail: string
  onClose: () => void
  onSuccess: (head: CommissionHeadDto) => void
}) {
  const { showError, showSuccess } = useToast()
  const [fullName, setFullName] = useState('')
  const [position, setPosition] = useState('')
  const [company, setCompany] = useState('')
  const [specialty, setSpecialty] = useState('')
  const mutation = useMutation({
    mutationFn: () =>
      createCommissionHead({
        secretaryEmail,
        fullName: fullName.trim(),
        position: position.trim(),
        company: company.trim(),
        specialty: specialty.trim(),
      }),
    onSuccess: (head) => {
      showSuccess()
      onSuccess(head)
    },
    onError: (error) => showError(getApiErrorMessages(error)),
  })
  const submit = () => {
    if (!fullName.trim() || !position.trim() || !company.trim() || !specialty.trim()) {
      showError('Заповніть дані голови комісії.')
      return
    }

    mutation.mutate()
  }

  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-blue-300/45 px-6 py-10 backdrop-blur-sm">
      <section className="w-full max-w-[620px] rounded-[26px] bg-white/90 p-10 shadow-2xl">
        <div className="flex justify-end">
          <button type="button" onClick={onClose} aria-label="Закрити" className="text-red-500">
            <X size={34} />
          </button>
        </div>
        <h2 className="text-center text-4xl font-bold text-slate-700">Додавання голови комісії</h2>
        <div className="mt-10 space-y-7">
          <label className="block space-y-3 text-center text-xl font-medium text-slate-500">
            <span>Введіть повний ПІБ</span>
            <input
              value={fullName}
              onChange={(event) => setFullName(event.target.value)}
              placeholder="Прізвище Ім’я По-батькові"
              className="h-12 w-full rounded-xl border border-slate-300 bg-transparent px-4 text-center text-lg font-bold outline-none placeholder:text-slate-400 focus:border-blue-500"
            />
          </label>
          <label className="block space-y-3 text-center text-xl font-medium text-slate-500">
            <span>Введіть посаду</span>
            <input
              value={position}
              onChange={(event) => setPosition(event.target.value)}
              placeholder="т.в.о. директора"
              className="h-12 w-full rounded-xl border border-slate-300 bg-transparent px-4 text-center text-lg font-bold outline-none placeholder:text-slate-400 focus:border-blue-500"
            />
          </label>
          <label className="block space-y-3 text-center text-xl font-medium text-slate-500">
            <span>Введіть підприємство</span>
            <input
              value={company}
              onChange={(event) => setCompany(event.target.value)}
              placeholder="Комунальне підприємство"
              className="h-12 w-full rounded-xl border border-slate-300 bg-transparent px-4 text-center text-lg font-bold outline-none placeholder:text-slate-400 focus:border-blue-500"
            />
          </label>
          <label className="block space-y-3 text-center text-xl font-medium text-slate-500">
            <span>Введіть спеціальність</span>
            <input
              value={specialty}
              onChange={(event) => setSpecialty(event.target.value)}
              placeholder="Інформаційні технології"
              className="h-12 w-full rounded-xl border border-slate-300 bg-transparent px-4 text-center text-lg font-bold outline-none placeholder:text-slate-400 focus:border-blue-500"
            />
          </label>
        </div>
        <button
          type="button"
          onClick={submit}
          disabled={mutation.isPending}
          className="mt-10 h-14 w-full rounded-full border-2 border-green-500 text-2xl font-bold text-green-600 transition hover:bg-green-500 hover:text-white disabled:opacity-50"
        >
          Додати
        </button>
      </section>
    </div>
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
  const optionsQuery = useQuery(commissionOptionsQuery(secretaryEmail, commission?.id))
  const [form, setForm] = useState<CommissionFormState>(() =>
    commission ? commissionFormFromResponse(commission) : makeDefaultCommissionForm(defenseYear),
  )
  const [isHeadDialogOpen, setIsHeadDialogOpen] = useState(false)
  const options = optionsQuery.data
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
      if (mode === 'create') {
        return createDiplomaExaminationCommission({
          secretaryEmail,
          secretaryId: form.secretaryId,
          orderNumber: form.orderNumber.trim(),
          educationLevel,
          defenseYear,
          commissionHeadId: form.commissionHeadId,
          firstMemberTeacherId: form.firstMemberTeacherId,
          secondMemberTeacherId: form.secondMemberTeacherId,
          thirdMemberTeacherId: form.thirdMemberTeacherId,
          startDate: form.startDate,
          endDate: form.endDate,
        })
      }

      return updateDiplomaExaminationCommission(commission?.id ?? '', {
        secretaryEmail,
        secretaryId: form.secretaryId,
        orderNumber: form.orderNumber.trim(),
        commissionHeadId: form.commissionHeadId,
        firstMemberTeacherId: form.firstMemberTeacherId,
        secondMemberTeacherId: form.secondMemberTeacherId,
        thirdMemberTeacherId: form.thirdMemberTeacherId,
        startDate: form.startDate,
        endDate: form.endDate,
      })
    },
    onSuccess: async (response) => {
      await queryClient.invalidateQueries({ queryKey: commissionQueryKeys.all })
      showSuccess()
      onSuccess(response)
    },
    onError: (error) => showError(getApiErrorMessages(error)),
  })

  const updateForm = (patch: Partial<CommissionFormState>) => setForm((current) => ({ ...current, ...patch }))
  const validate = () => {
    const messages: string[] = []
    const memberIds = [form.firstMemberTeacherId, form.secondMemberTeacherId, form.thirdMemberTeacherId]

    if (!form.orderNumber.trim()) {
      messages.push('Вкажіть № комісії.')
    }
    if (!form.commissionHeadId) {
      messages.push('Оберіть голову комісії.')
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
    if (form.startDate && !form.startDate.startsWith(`${defenseYear}-`)) {
      messages.push('Початок роботи має належати обраному року захисту.')
    }
    if (form.endDate && !form.endDate.startsWith(`${defenseYear}-`)) {
      messages.push('Кінець роботи має належати обраному року захисту.')
    }
    if (form.startDate && form.endDate && form.endDate < form.startDate) {
      messages.push('Кінець роботи має бути не раніше початку.')
    }
    if (memberIds.filter(Boolean).length !== new Set(memberIds.filter(Boolean)).size) {
      messages.push('Члени комісії мають бути різними викладачами.')
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
  const handleHeadCreated = async (head: CommissionHeadDto) => {
    await queryClient.invalidateQueries({ queryKey: commissionQueryKeys.all })
    updateForm({ commissionHeadId: asString(head.id) })
    setIsHeadDialogOpen(false)
  }

  return (
    <div className="fixed inset-0 z-40 overflow-hidden bg-[#dcecff]/80 px-6 py-10 backdrop-blur-sm">
      <section className="mx-auto max-h-[calc(100vh-80px)] max-w-[1280px] overflow-y-auto rounded-[28px] bg-white/80 p-10 shadow-xl">
        <div className="flex items-start justify-between">
          <h2 className="text-4xl font-bold uppercase text-blue-600">
            {mode === 'create' ? 'Створення екзаменаційної комісії' : 'Зміна екзаменаційної комісії'}
          </h2>
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
              <CommissionTextField
                label="№ комісії"
                value={form.orderNumber}
                onChange={(orderNumber) => updateForm({ orderNumber })}
              />
            </section>

            <section className="space-y-5">
              <h3 className="text-sm font-bold uppercase text-slate-500">Склад комісії</h3>
              <div className="grid grid-cols-[170px_1fr] items-center gap-6 text-lg font-bold text-slate-700">
                <span>Голова комісії</span>
                <div className="flex gap-3">
                  <select
                    value={form.commissionHeadId}
                    onChange={(event) => updateForm({ commissionHeadId: event.target.value })}
                    className="h-12 min-w-0 flex-1 rounded-xl border border-slate-300 bg-transparent px-4 outline-none focus:border-blue-500"
                  >
                    <option value="">Оберіть голову</option>
                    {options.commissionHeads.map((head) => (
                      <option key={head.id} value={head.id}>
                        {head.fullName}
                      </option>
                    ))}
                  </select>
                  <button
                    type="button"
                    onClick={() => setIsHeadDialogOpen(true)}
                    className="h-12 shrink-0 rounded-full border-2 border-blue-600 px-6 font-bold text-blue-600 transition hover:bg-blue-600 hover:text-white"
                  >
                    + Додати персону
                  </button>
                </div>
              </div>

              <CommissionSelectField
                label="1."
                value={form.firstMemberTeacherId}
                onChange={(firstMemberTeacherId) => updateForm({ firstMemberTeacherId })}
              >
                <option value="">Оберіть викладача</option>
                {options.teachers.map((teacher) => (
                  <option
                    key={teacher.id}
                    value={teacher.id}
                    disabled={
                      selectedMemberIds.includes(asString(teacher.id)) &&
                      asString(teacher.id) !== form.firstMemberTeacherId
                    }
                  >
                    {teacher.fullName}
                  </option>
                ))}
              </CommissionSelectField>
              <CommissionSelectField
                label="2."
                value={form.secondMemberTeacherId}
                onChange={(secondMemberTeacherId) => updateForm({ secondMemberTeacherId })}
              >
                <option value="">Оберіть викладача</option>
                {options.teachers.map((teacher) => (
                  <option
                    key={teacher.id}
                    value={teacher.id}
                    disabled={
                      selectedMemberIds.includes(asString(teacher.id)) &&
                      asString(teacher.id) !== form.secondMemberTeacherId
                    }
                  >
                    {teacher.fullName}
                  </option>
                ))}
              </CommissionSelectField>
              <CommissionSelectField
                label="3."
                value={form.thirdMemberTeacherId}
                onChange={(thirdMemberTeacherId) => updateForm({ thirdMemberTeacherId })}
              >
                <option value="">Оберіть викладача</option>
                {options.teachers.map((teacher) => (
                  <option
                    key={teacher.id}
                    value={teacher.id}
                    disabled={
                      selectedMemberIds.includes(asString(teacher.id)) &&
                      asString(teacher.id) !== form.thirdMemberTeacherId
                    }
                  >
                    {teacher.fullName}
                  </option>
                ))}
              </CommissionSelectField>
              <CommissionSelectField
                label="Секретар комісії"
                value={form.secretaryId}
                onChange={(secretaryId) => updateForm({ secretaryId })}
              >
                <option value="">Оберіть секретаря</option>
                {options.secretaries.map((secretary) => (
                  <option key={secretary.id} value={secretary.id}>
                    {secretary.fullName}
                  </option>
                ))}
              </CommissionSelectField>
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
                className="h-12 rounded-full border-2 border-blue-600 px-8 text-lg font-bold text-blue-600 transition hover:bg-blue-600 hover:text-white"
              >
                Скасувати
              </button>
              <button
                type="button"
                onClick={submit}
                disabled={mutation.isPending}
                className="h-12 rounded-full border-2 border-green-500 px-8 text-lg font-bold text-green-600 transition hover:bg-green-500 hover:text-white disabled:opacity-50 disabled:hover:bg-transparent disabled:hover:text-green-600"
              >
                {mode === 'create' ? 'Створити' : 'Зберегти'}
              </button>
            </div>
          </div>
        )}
      </section>

      {isHeadDialogOpen && (
        <AddCommissionHeadDialog
          secretaryEmail={secretaryEmail}
          onClose={() => setIsHeadDialogOpen(false)}
          onSuccess={handleHeadCreated}
        />
      )}
    </div>
  )
}

export function GroupsPage() {
  const { defenseYear = '', groupId, view, studentId } = useParams()
  const [searchParams, setSearchParams] = useSearchParams()
  const location = useLocation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { secretaryEmail } = useAuth()
  const { showError, showSuccess } = useToast()
  const educationLevel: EducationLevel = searchParams.get('level') === 'Master' ? 'Master' : 'Bachelor'
  const yearsQuery = useQuery(academicYearsQuery(secretaryEmail, educationLevel))
  const years = yearsQuery.data ?? []
  const selectedYear = years.find((item) => item.defenseYear === defenseYear) ?? years[0]
  const isCommissionView = Boolean(defenseYear && location.pathname.endsWith(`/groups/${defenseYear}/commission`))
  const selectedGroup = useMemo(() => {
    if (!selectedYear || isCommissionView) {
      return undefined
    }

    return selectedYear.groups.find((group) => asString(group.id) === groupId) ?? selectedYear.groups[0]
  }, [groupId, isCommissionView, selectedYear])
  const selectedGroupId = selectedGroup?.id
  const studentsQuery = useQuery(groupStudentsQuery(selectedGroupId, secretaryEmail))
  const commissionQuery = useQuery(commissionsQuery(secretaryEmail, educationLevel, selectedYear?.defenseYear ?? ''))
  const commission = commissionQuery.data
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [isEditOpen, setIsEditOpen] = useState(false)
  const [isAddStudentOpen, setIsAddStudentOpen] = useState(false)
  const [isCreateCommissionOpen, setIsCreateCommissionOpen] = useState(false)
  const [isEditCommissionOpen, setIsEditCommissionOpen] = useState(false)
  const [groupToDelete, setGroupToDelete] = useState<GroupDto | null>(null)
  const [studentToDelete, setStudentToDelete] = useState<StudentDetailsResponse | null>(null)
  const [commissionToDelete, setCommissionToDelete] = useState<DiplomaExaminationCommissionResponse | null>(null)
  const deleteGroupMutation = useMutation({
    mutationFn: (group: GroupDto) => deleteGroup(group.id, secretaryEmail),
    onSuccess: async () => {
      setGroupToDelete(null)
      await queryClient.invalidateQueries({ queryKey: groupsQueryKeys.all })
      navigate(makePath(defenseYear ? `/groups/${defenseYear}` : '/groups', educationLevel), { replace: true })
      showSuccess()
    },
    onError: (error) => showError(getApiErrorMessages(error)),
  })
  const deleteStudentMutation = useMutation({
    mutationFn: (student: StudentDetailsResponse) => deleteStudent(student.id, secretaryEmail),
    onSuccess: async () => {
      setStudentToDelete(null)
      await queryClient.invalidateQueries({ queryKey: groupsQueryKeys.all })
      navigate(makePath(`/groups/${defenseYear}/${selectedGroupId}`, educationLevel), { replace: true })
      showSuccess()
    },
    onError: (error) => showError(getApiErrorMessages(error)),
  })
  const deleteCommissionMutation = useMutation({
    mutationFn: (currentCommission: DiplomaExaminationCommissionResponse) =>
      deleteDiplomaExaminationCommission(currentCommission.id, secretaryEmail),
    onSuccess: async () => {
      setCommissionToDelete(null)
      await queryClient.invalidateQueries({ queryKey: commissionQueryKeys.all })
      navigate(makePath(`/groups/${selectedYear?.defenseYear ?? defenseYear}`, educationLevel), { replace: true })
      showSuccess()
    },
    onError: (error) => showError(getApiErrorMessages(error)),
  })

  const handleEducationChange = (nextLevel: EducationLevel) => {
    setSearchParams({ level: nextLevel })
  }
  const handleMutationSuccess = async (nextDefenseYear?: string, nextGroupId?: EntityId) => {
    setIsCreateOpen(false)
    setIsEditOpen(false)
    setIsAddStudentOpen(false)
    await queryClient.invalidateQueries({ queryKey: groupsQueryKeys.all })

    if (nextDefenseYear && nextGroupId) {
      navigate(makePath(`/groups/${nextDefenseYear}/${nextGroupId}`, educationLevel))
    }
  }
  const handleCommissionSuccess = async (nextCommission: DiplomaExaminationCommissionResponse) => {
    setIsCreateCommissionOpen(false)
    setIsEditCommissionOpen(false)
    await queryClient.invalidateQueries({ queryKey: commissionQueryKeys.all })
    navigate(makePath(`/groups/${nextCommission.defenseYear}/commission`, educationLevel))
  }
  const isExpandedGroupView =
    view === 'admission' || view === 'material-components' || view === 'electronic-components' || view === 'results'

  return (
    <section className="space-y-12">
      <TopControls
        educationLevel={educationLevel}
        onEducationChange={handleEducationChange}
        onCreateGroup={() => setIsCreateOpen(true)}
      />

      {yearsQuery.isLoading && <SectionMessage>Завантажуємо групи...</SectionMessage>}
      {yearsQuery.error && <ErrorMessage error={yearsQuery.error} />}
      {!yearsQuery.isLoading && !yearsQuery.error && years.length === 0 && (
        <SectionMessage>Груп для цього секретаря та ОКР не знайдено.</SectionMessage>
      )}

      {!defenseYear && years.length > 0 && <YearCards years={years} educationLevel={educationLevel} />}

      {defenseYear && selectedYear && !studentId && isExpandedGroupView && selectedGroup && studentsQuery.data && (
        <>
          {view === 'admission' && (
            <AdmissionScreen
              students={studentsQuery.data}
              group={selectedGroup}
              educationLevel={educationLevel}
              defenseYear={selectedYear.defenseYear}
            />
          )}
          {view === 'material-components' && (
            <ChecklistTable
              title="Матеріальні компоненти"
              students={studentsQuery.data}
              type="physical"
              group={selectedGroup}
              educationLevel={educationLevel}
              defenseYear={selectedYear.defenseYear}
            />
          )}
          {view === 'electronic-components' && (
            <ChecklistTable
              title="Електронні компоненти"
              students={studentsQuery.data}
              type="electronic"
              group={selectedGroup}
              educationLevel={educationLevel}
              defenseYear={selectedYear.defenseYear}
            />
          )}
          {view === 'results' && (
            <ResultsScreen
              group={selectedGroup}
              educationLevel={educationLevel}
              defenseYear={selectedYear.defenseYear}
              secretaryEmail={secretaryEmail}
            />
          )}
        </>
      )}

      {defenseYear && selectedYear && !studentId && isExpandedGroupView && selectedGroup && studentsQuery.isLoading && (
        <SectionMessage>Завантажуємо студентів...</SectionMessage>
      )}

      {defenseYear && selectedYear && !studentId && isExpandedGroupView && selectedGroup && studentsQuery.error && (
        <ErrorMessage error={studentsQuery.error} />
      )}

      {defenseYear && selectedYear && !studentId && !isExpandedGroupView && (
        <>
          <div className="grid grid-cols-[320px_1fr] gap-7">
            <div className="space-y-8">
              <YearTabs years={years} activeDefenseYear={selectedYear.defenseYear} educationLevel={educationLevel} />
              <GroupSidebar
                groups={selectedYear.groups}
                selectedGroupId={selectedGroupId}
                educationLevel={educationLevel}
                defenseYear={selectedYear.defenseYear}
                commission={commission}
                isCommissionSelected={isCommissionView}
                onCreateCommission={() => setIsCreateCommissionOpen(true)}
              />
            </div>

            {isCommissionView && commissionQuery.isLoading && <SectionMessage>Завантажуємо комісію...</SectionMessage>}
            {isCommissionView && commissionQuery.error && !isApiNotFound(commissionQuery.error) && (
              <ErrorMessage error={commissionQuery.error} />
            )}
            {isCommissionView && commission && (
              <CommissionDetails
                commission={commission}
                onEdit={() => setIsEditCommissionOpen(true)}
                onDelete={() => setCommissionToDelete(commission)}
              />
            )}
            {isCommissionView && !commissionQuery.isLoading && !commissionQuery.error && !commission && (
              <SectionMessage>Для цього року ще не створено екзаменаційну комісію.</SectionMessage>
            )}
            {isCommissionView && isApiNotFound(commissionQuery.error) && (
              <SectionMessage>Для цього року ще не створено екзаменаційну комісію.</SectionMessage>
            )}

            {!isCommissionView && !selectedGroup && <SectionMessage>Оберіть або створіть групу.</SectionMessage>}
            {!isCommissionView && selectedGroup && studentsQuery.isLoading && <SectionMessage>Завантажуємо студентів...</SectionMessage>}
            {!isCommissionView && selectedGroup && studentsQuery.error && <ErrorMessage error={studentsQuery.error} />}
            {!isCommissionView && selectedGroup && studentsQuery.data && !view && (
              <GroupOverview
                group={selectedGroup}
                students={studentsQuery.data}
                educationLevel={educationLevel}
                defenseYear={selectedYear.defenseYear}
                onEditGroup={() => setIsEditOpen(true)}
                onDeleteGroup={() => setGroupToDelete(selectedGroup)}
                onAddStudent={() => setIsAddStudentOpen(true)}
              />
            )}
            {!isCommissionView && selectedGroup && studentsQuery.data && view === 'admission' && (
              <AdmissionScreen
                students={studentsQuery.data}
                group={selectedGroup}
                educationLevel={educationLevel}
                defenseYear={selectedYear.defenseYear}
              />
            )}
            {!isCommissionView && selectedGroup && studentsQuery.data && view === 'material-components' && (
              <ChecklistTable
                title="Матеріальні компоненти"
                students={studentsQuery.data}
                type="physical"
                group={selectedGroup}
                educationLevel={educationLevel}
                defenseYear={selectedYear.defenseYear}
              />
            )}
            {!isCommissionView && selectedGroup && studentsQuery.data && view === 'electronic-components' && (
              <ChecklistTable
                title="Електронні компоненти"
                students={studentsQuery.data}
                type="electronic"
                group={selectedGroup}
                educationLevel={educationLevel}
                defenseYear={selectedYear.defenseYear}
              />
            )}
            {!isCommissionView && selectedGroup && view === 'results' && (
              <ResultsScreen
                group={selectedGroup}
                educationLevel={educationLevel}
                defenseYear={selectedYear.defenseYear}
                secretaryEmail={secretaryEmail}
              />
            )}
          </div>
        </>
      )}

      {defenseYear && selectedYear && selectedGroup && studentId && (
        <StudentDetailsPanel
          studentId={studentId}
          group={selectedGroup}
          years={years}
          students={studentsQuery.data ?? []}
          educationLevel={educationLevel}
          defenseYear={selectedYear.defenseYear}
          secretaryEmail={secretaryEmail}
          onDeleteStudent={setStudentToDelete}
        />
      )}

      {isCreateOpen && (
        <GroupDialog
          mode="create"
          educationLevel={educationLevel}
          secretaryEmail={secretaryEmail}
          onClose={() => setIsCreateOpen(false)}
          onSuccess={handleMutationSuccess}
        />
      )}
      {isEditOpen && selectedGroup && (
        <GroupDialog
          mode="edit"
          educationLevel={educationLevel}
          initialGroup={selectedGroup}
          initialYear={selectedYear.defenseYear}
          secretaryEmail={secretaryEmail}
          onClose={() => setIsEditOpen(false)}
          onSuccess={handleMutationSuccess}
        />
      )}
      {isAddStudentOpen && selectedGroupId && (
        <AddStudentDialog
          groupId={selectedGroupId}
          secretaryEmail={secretaryEmail}
          onClose={() => setIsAddStudentOpen(false)}
          onSuccess={() => handleMutationSuccess(defenseYear, selectedGroupId)}
        />
      )}
      {isCreateCommissionOpen && selectedYear && (
        <CommissionFormDialog
          mode="create"
          secretaryEmail={secretaryEmail}
          educationLevel={educationLevel}
          defenseYear={selectedYear.defenseYear}
          onClose={() => setIsCreateCommissionOpen(false)}
          onSuccess={handleCommissionSuccess}
        />
      )}
      {isEditCommissionOpen && commission && (
        <CommissionFormDialog
          mode="edit"
          secretaryEmail={secretaryEmail}
          educationLevel={educationLevel}
          defenseYear={commission.defenseYear}
          commission={commission}
          onClose={() => setIsEditCommissionOpen(false)}
          onSuccess={handleCommissionSuccess}
        />
      )}
      {groupToDelete && (
        <ConfirmDialog
          title="Видалення групи"
          confirmLabel="Видалити"
          onConfirm={() => deleteGroupMutation.mutate(groupToDelete)}
          onCancel={() => setGroupToDelete(null)}
        >
          Ви впевнені, що хочете видалити {groupToDelete.name}? Цю дію неможливо скасувати.
        </ConfirmDialog>
      )}
      {studentToDelete && (
        <ConfirmDialog
          title="Видалення студента"
          confirmLabel="Видалити"
          onConfirm={() => deleteStudentMutation.mutate(studentToDelete)}
          onCancel={() => setStudentToDelete(null)}
        >
          Ви впевнені, що хочете видалити студента {studentToDelete.fullName}? Цю дію неможливо скасувати.
        </ConfirmDialog>
      )}
      {commissionToDelete && (
        <ConfirmDialog
          title="Видалення комісії"
          confirmLabel="Видалити"
          onConfirm={() => deleteCommissionMutation.mutate(commissionToDelete)}
          onCancel={() => setCommissionToDelete(null)}
        >
          Ви впевнені, що хочете видалити ЕК №{commissionToDelete.orderNumber}? Цю дію неможливо скасувати.
        </ConfirmDialog>
      )}
    </section>
  )
}
