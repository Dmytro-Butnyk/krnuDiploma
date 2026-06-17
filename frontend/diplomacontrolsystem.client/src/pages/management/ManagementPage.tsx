import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CheckCircle2, RotateCcw, ShieldCheck, Trash2, UserPlus, X } from 'lucide-react'
import { useMemo, useState, type FormEvent, type ReactNode } from 'react'
import { Navigate, useSearchParams } from 'react-router-dom'
import { useAuth } from '../../features/auth/model/useAuth'
import {
  createCommissionHead,
  deleteCommissionHead,
  updateCommissionHead,
} from '../../features/commissions/api/commissionsApi'
import type { CommissionHeadDto } from '../../features/commissions/api/types'
import {
  commissionHeadsQuery,
  commissionQueryKeys,
} from '../../features/commissions/model/commissionsQueries'
import type { EntityId, PersonNameFormsDto } from '../../features/groups/api/types'
import {
  createAcademicDegree,
  createSecretary,
  createSpecialty,
  createTeacher,
  createTeacherPosition,
  deleteAcademicDegree,
  deleteSecretary,
  deleteSpecialty,
  deleteTeacher,
  deleteTeacherPosition,
  hardDeleteSecretary,
  restoreAcademicDegree,
  restoreSecretary,
  restoreSpecialty,
  restoreTeacher,
  restoreTeacherPosition,
  updateAcademicDegree,
  updateSecretary,
  updateSpecialty,
  updateTeacher,
  updateTeacherPosition,
} from '../../features/management/api/managementApi'
import type {
  AcademicDegreeDto,
  SecretaryDto,
  SpecialtyDto,
  TeacherDto,
  TeacherPositionDto,
  UpsertSecretaryRequest,
  UpsertTeacherRequest,
} from '../../features/management/api/types'
import {
  academicDegreesQuery,
  managementQueryKeys,
  secretariesQuery,
  specialtiesQuery,
  teacherPositionsQuery,
  teachersQuery,
} from '../../features/management/model/managementQueries'
import { getApiErrorMessage } from '../../shared/api/errorMessage'
import { ConfirmDialog } from '../../shared/ui/ConfirmDialog'
import { useToast } from '../../shared/ui/toast/ToastContext'

type CatalogTab = 'degrees' | 'positions' | 'specialties' | 'commission-heads'
type PanelMode =
  | 'details'
  | 'lookup-create'
  | 'lookup-edit'
  | 'commission-head-create'
  | 'commission-head-edit'
  | 'specialty-create'
  | 'specialty-edit'
  | 'specialty-teachers'
  | 'specialty-secretaries'
  | 'teacher-create'
  | 'teacher-edit'
  | 'secretary-create'
  | 'secretary-edit'

interface LookupFormState {
  fullName: string
  shortName: string
  genitiveFullName: string
  genitiveShortName: string
  isActive: boolean
}

interface CommissionHeadFormState {
  fullName: string
  nameForms: PersonNameFormsDto
  position: string
  company: string
  specialty: string
}

interface SpecialtyFormState {
  code: string
  name: string
  isActive: boolean
}

interface SecretaryFormState {
  email: string
  fullName: string
  specialtyId: string
  isActive: boolean
  isSuperSecretary: boolean
}

interface TeacherFormState {
  fullName: string
  shortName: string
  nameForms: PersonNameFormsDto
  email: string
  phoneNumber: string
  academicDegreeId: string
  teacherPositionId: string
  specialtyId: string
  isActive: boolean
}

interface ConfirmState {
  title: string
  message: ReactNode
  confirmLabel: string
  onConfirm: () => void
}

type SidebarItem = AcademicDegreeDto | TeacherPositionDto | SpecialtyDto | CommissionHeadDto

const tabs: Array<{ value: CatalogTab; label: string }> = [
  { value: 'degrees', label: 'Ступені' },
  { value: 'positions', label: 'Посади' },
  { value: 'specialties', label: 'Спеціальності' },
  { value: 'commission-heads', label: 'Голови ДЕК' },
]

const emptyLookupForm: LookupFormState = {
  fullName: '',
  shortName: '',
  genitiveFullName: '',
  genitiveShortName: '',
  isActive: true,
}

const emptyNameForms: PersonNameFormsDto = {
  nominative: '',
  genitive: '',
  dative: '',
  signature: '',
}

const emptyCommissionHeadForm: CommissionHeadFormState = {
  fullName: '',
  nameForms: emptyNameForms,
  position: '',
  company: '',
  specialty: '',
}

const nameFormFields: Array<{ key: keyof PersonNameFormsDto; label: string }> = [
  { key: 'nominative', label: 'Називний відмінок' },
  { key: 'genitive', label: 'Родовий відмінок' },
  { key: 'dative', label: 'Давальний відмінок' },
  { key: 'signature', label: 'Підпис' },
]

const emptySpecialtyForm: SpecialtyFormState = {
  code: '',
  name: '',
  isActive: true,
}

const fullNameValidationMessage =
  "ПІБ має містити рівно прізвище, ім'я та по батькові кирилицею, кожне слово з великої літери. Дозволені лише дефіс і апостроф."

function idEquals(left: EntityId, right: EntityId | string | undefined) {
  return String(left) === String(right ?? '')
}

function tidyText(value: string) {
  return value.trim().replace(/\s+/g, ' ')
}

function makeDefaultNameForms(fullName: string, signature = fullName): PersonNameFormsDto {
  return {
    nominative: fullName,
    genitive: fullName,
    dative: fullName,
    signature,
  }
}

function normalizeNameForms(
  nameForms: PersonNameFormsDto | null | undefined,
  fallbackFullName: string,
  fallbackSignature = fallbackFullName,
): PersonNameFormsDto {
  const fallback = makeDefaultNameForms(fallbackFullName, fallbackSignature)

  return {
    nominative: nameForms?.nominative ?? fallback.nominative,
    genitive: nameForms?.genitive ?? fallback.genitive,
    dative: nameForms?.dative ?? fallback.dative,
    signature: nameForms?.signature ?? fallback.signature,
  }
}

function cleanNameForms(nameForms: PersonNameFormsDto): PersonNameFormsDto {
  return {
    nominative: tidyText(nameForms.nominative),
    genitive: tidyText(nameForms.genitive),
    dative: tidyText(nameForms.dative),
    signature: tidyText(nameForms.signature),
  }
}

function validateNameForms(nameForms: PersonNameFormsDto, toast: ReturnType<typeof useToast>) {
  if (Object.values(cleanNameForms(nameForms)).some((value) => value.length > 256)) {
    toast.showError('Форми ПІБ для документів мають бути не довші за 256 символів.')
    return false
  }

  return true
}

function normalizeFullNameInput(value: string) {
  const normalized = value
    .replace(/[’ʼ`´]/g, "'")
    .replace(/[^А-ЯЄІЇҐа-яєіїґ'\-\s]/gu, '')
    .replace(/\s+/g, ' ')
    .replace(/\s*-\s*/g, '-')
    .replace(/\s*'\s*/g, "'")
    .replace(/-{2,}/g, '-')
    .replace(/'{2,}/g, "'")
  const hasTrailingSpace = normalized.endsWith(' ')
  const parts = normalized.trimStart().split(' ').filter(Boolean).slice(0, 3)

  if (parts.length === 0) {
    return ''
  }

  const limited = parts.join(' ')

  return hasTrailingSpace && parts.length < 3 ? `${limited} ` : limited
}

function isCapitalizedNamePart(part: string) {
  return part
    .split('-')
    .every((segment) => /^[А-ЯЄІЇҐ][а-яєіїґ]*(?:'[а-яєіїґ]+)?$/u.test(segment))
}

function isValidFullName(value: string) {
  const parts = tidyText(value).split(' ')

  return parts.length === 3 && parts.every(isCapitalizedNamePart)
}

function makeTeacherShortName(fullName: string) {
  const parts = tidyText(fullName).split(' ')

  if (parts.length !== 3 || parts.some((part) => !part)) {
    return ''
  }

  const [lastName, firstName, middleName] = parts

  return `${lastName} ${firstName[0].toLocaleUpperCase('uk-UA')}. ${middleName[0].toLocaleUpperCase('uk-UA')}.`
}

function lookupLabel(item: AcademicDegreeDto | TeacherPositionDto) {
  return item.shortName || item.fullName
}

function sidebarLabel(item: SidebarItem) {
  if ('company' in item) {
    return item.fullName
  }

  if ('shortName' in item) {
    return lookupLabel(item)
  }

  return item.code
}

function isSidebarItemInactive(item: SidebarItem) {
  return 'isDeleted' in item ? item.isDeleted : !item.isActive
}

function itemStatus(isActive: boolean) {
  return isActive ? 'Активний' : 'Архів'
}

function activeClass(isActive: boolean) {
  return isActive ? 'text-green-600' : 'text-slate-400'
}

function makeSecretaryForm(secretary: SecretaryDto | null, specialtyId: EntityId | undefined): SecretaryFormState {
  return {
    email: secretary?.email ?? '',
    fullName: secretary?.fullName ?? '',
    specialtyId: String(secretary?.specialtyId ?? specialtyId ?? ''),
    isActive: secretary?.isActive ?? true,
    isSuperSecretary: secretary?.isSuperSecretary ?? false,
  }
}

function makeTeacherForm(
  teacher: TeacherDto | null,
  specialtyId: EntityId | undefined,
  degreeId: EntityId | undefined,
  positionId: EntityId | undefined,
): TeacherFormState {
  const fullName = teacher?.fullName ?? ''

  return {
    fullName,
    shortName: makeTeacherShortName(fullName),
    nameForms: normalizeNameForms(teacher?.nameForms, fullName, teacher?.shortName ?? makeTeacherShortName(fullName)),
    email: teacher?.email ?? '',
    phoneNumber: teacher?.phoneNumber ?? '',
    academicDegreeId: String(teacher?.academicDegreeId ?? degreeId ?? ''),
    teacherPositionId: String(teacher?.teacherPositionId ?? positionId ?? ''),
    specialtyId: String(teacher?.specialtyId ?? specialtyId ?? ''),
    isActive: teacher?.isActive ?? true,
  }
}

function makeCommissionHeadForm(commissionHead: CommissionHeadDto | null): CommissionHeadFormState {
  const fullName = commissionHead?.fullName ?? ''

  return {
    fullName,
    nameForms: normalizeNameForms(commissionHead?.nameForms, fullName),
    position: commissionHead?.position ?? '',
    company: commissionHead?.company ?? '',
    specialty: commissionHead?.specialty ?? '',
  }
}

function toEntityId(value: string): EntityId {
  const parsed = Number(value)

  return Number.isFinite(parsed) && String(parsed) === value ? parsed : value
}

function isLookupTab(tab: CatalogTab): tab is 'degrees' | 'positions' {
  return tab === 'degrees' || tab === 'positions'
}

function normalizeCatalogTab(value: string | null): CatalogTab {
  return value === 'degrees' || value === 'positions' || value === 'specialties' || value === 'commission-heads'
    ? value
    : 'specialties'
}

export function ManagementPage() {
  const { secretary } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const [searchParams] = useSearchParams()
  const activeTab = normalizeCatalogTab(searchParams.get('tab'))
  const [panelState, setPanelState] = useState<{ tab: CatalogTab; mode: PanelMode }>({ tab: activeTab, mode: 'details' })
  const panelMode = panelState.tab === activeTab ? panelState.mode : 'details'
  const setPanelMode = (mode: PanelMode) => setPanelState({ tab: activeTab, mode })
  const [selectedDegreeId, setSelectedDegreeId] = useState<EntityId | undefined>()
  const [selectedPositionId, setSelectedPositionId] = useState<EntityId | undefined>()
  const [selectedSpecialtyId, setSelectedSpecialtyId] = useState<EntityId | undefined>()
  const [selectedTeacherId, setSelectedTeacherId] = useState<EntityId | undefined>()
  const [selectedSecretaryId, setSelectedSecretaryId] = useState<EntityId | undefined>()
  const [selectedCommissionHeadId, setSelectedCommissionHeadId] = useState<EntityId | undefined>()
  const [lookupForm, setLookupForm] = useState<LookupFormState>(emptyLookupForm)
  const [commissionHeadForm, setCommissionHeadForm] = useState<CommissionHeadFormState>(emptyCommissionHeadForm)
  const [specialtyForm, setSpecialtyForm] = useState<SpecialtyFormState>(emptySpecialtyForm)
  const [teacherForm, setTeacherForm] = useState<TeacherFormState>(() => makeTeacherForm(null, undefined, undefined, undefined))
  const [secretaryForm, setSecretaryForm] = useState<SecretaryFormState>(() => makeSecretaryForm(null, undefined))
  const [confirmState, setConfirmState] = useState<ConfirmState | null>(null)

  const degreesQuery = useQuery(academicDegreesQuery())
  const positionsQuery = useQuery(teacherPositionsQuery())
  const specialtiesQueryResult = useQuery(specialtiesQuery())
  const secretariesQueryResult = useQuery(secretariesQuery())
  const commissionHeadsQueryResult = useQuery(commissionHeadsQuery(secretary?.email ?? ''))

  const degrees = useMemo(() => degreesQuery.data ?? [], [degreesQuery.data])
  const positions = useMemo(() => positionsQuery.data ?? [], [positionsQuery.data])
  const specialties = useMemo(() => specialtiesQueryResult.data ?? [], [specialtiesQueryResult.data])
  const secretaries = useMemo(() => secretariesQueryResult.data ?? [], [secretariesQueryResult.data])
  const commissionHeads = useMemo(() => commissionHeadsQueryResult.data ?? [], [commissionHeadsQueryResult.data])
  const effectiveSelectedDegreeId = selectedDegreeId ?? degrees[0]?.id
  const effectiveSelectedPositionId = selectedPositionId ?? positions[0]?.id
  const effectiveSelectedSpecialtyId = selectedSpecialtyId ?? specialties[0]?.id
  const effectiveSelectedCommissionHeadId = selectedCommissionHeadId ?? commissionHeads[0]?.id
  const teachersQueryResult = useQuery(teachersQuery(effectiveSelectedSpecialtyId))
  const teachers = useMemo(() => teachersQueryResult.data ?? [], [teachersQueryResult.data])
  const selectedDegree = degrees.find((degree) => idEquals(degree.id, effectiveSelectedDegreeId))
  const selectedPosition = positions.find((position) => idEquals(position.id, effectiveSelectedPositionId))
  const selectedSpecialty = specialties.find((specialty) => idEquals(specialty.id, effectiveSelectedSpecialtyId))
  const selectedTeacher = teachers.find((teacher) => idEquals(teacher.id, selectedTeacherId))
  const selectedSecretary = secretaries.find((item) => idEquals(item.id, selectedSecretaryId))
  const selectedCommissionHead = commissionHeads.find((head) => idEquals(head.id, effectiveSelectedCommissionHeadId))
  const currentLookupItem = activeTab === 'degrees' ? selectedDegree : activeTab === 'positions' ? selectedPosition : undefined
  const specialtySecretaries = useMemo(
    () => secretaries.filter((item) => selectedSpecialty && idEquals(item.specialtyId, selectedSpecialty.id)),
    [secretaries, selectedSpecialty],
  )

  const runMutation = useMutation({
    mutationFn: (action: () => Promise<unknown>) => action(),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: managementQueryKeys.all }),
        queryClient.invalidateQueries({ queryKey: managementQueryKeys.teachers(effectiveSelectedSpecialtyId) }),
        queryClient.invalidateQueries({ queryKey: commissionQueryKeys.all }),
      ])
      toast.showSuccess()
      setPanelMode('details')
      setConfirmState(null)
    },
    onError: (error) => {
      toast.showError(getApiErrorMessage(error))
      setConfirmState(null)
    },
  })

  if (!secretary?.isSuperSecretary) {
    return <Navigate to="/groups" replace />
  }

  const beginCreate = () => {
    if (isLookupTab(activeTab)) {
      setLookupForm(emptyLookupForm)
      setPanelMode('lookup-create')
      return
    }

    if (activeTab === 'commission-heads') {
      setSelectedCommissionHeadId(undefined)
      setCommissionHeadForm(emptyCommissionHeadForm)
      setPanelMode('commission-head-create')
      return
    }

    setSpecialtyForm(emptySpecialtyForm)
    setPanelMode('specialty-create')
  }

  const beginLookupEdit = (item: AcademicDegreeDto | TeacherPositionDto) => {
    setLookupForm({
      fullName: item.fullName,
      shortName: item.shortName,
      genitiveFullName: item.genitiveFullName,
      genitiveShortName: item.genitiveShortName,
      isActive: item.isActive,
    })
    setPanelMode('lookup-edit')
  }

  const beginCommissionHeadEdit = (item: CommissionHeadDto) => {
    setSelectedCommissionHeadId(item.id)
    setCommissionHeadForm(makeCommissionHeadForm(item))
    setPanelMode('commission-head-edit')
  }

  const beginSpecialtyEdit = (specialty: SpecialtyDto) => {
    setSelectedSpecialtyId(specialty.id)
    setSpecialtyForm({
      code: specialty.code,
      name: specialty.name,
      isActive: specialty.isActive,
    })
    setPanelMode('specialty-edit')
  }

  const beginSpecialtyTeachers = (specialty: SpecialtyDto) => {
    setSelectedSpecialtyId(specialty.id)
    setSelectedTeacherId(undefined)
    setPanelMode('specialty-teachers')
  }

  const beginSpecialtySecretaries = (specialty: SpecialtyDto) => {
    setSelectedSpecialtyId(specialty.id)
    setSelectedSecretaryId(undefined)
    setPanelMode('specialty-secretaries')
  }

  const beginTeacherCreate = () => {
    setSelectedTeacherId(undefined)
    setTeacherForm(makeTeacherForm(null, selectedSpecialty?.id, degrees[0]?.id, positions[0]?.id))
    setPanelMode('teacher-create')
  }

  const beginTeacherEdit = (teacher: TeacherDto) => {
    setSelectedTeacherId(teacher.id)
    setTeacherForm(makeTeacherForm(teacher, selectedSpecialty?.id, degrees[0]?.id, positions[0]?.id))
    setPanelMode('teacher-edit')
  }

  const beginSecretaryCreate = () => {
    setSelectedSecretaryId(undefined)
    setSecretaryForm(makeSecretaryForm(null, selectedSpecialty?.id))
    setPanelMode('secretary-create')
  }

  const beginSecretaryEdit = (item: SecretaryDto) => {
    setSelectedSecretaryId(item.id)
    setSecretaryForm(makeSecretaryForm(item, selectedSpecialty?.id))
    setPanelMode('secretary-edit')
  }

  const submitLookup = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    const request = {
      fullName: lookupForm.fullName.trim(),
      shortName: lookupForm.shortName.trim(),
      genitiveFullName: lookupForm.genitiveFullName.trim() || null,
      genitiveShortName: lookupForm.genitiveShortName.trim() || null,
      isActive: lookupForm.isActive,
    }

    if (request.genitiveFullName && request.genitiveFullName.length > 256) {
      toast.showError('Повна назва у родовому відмінку має бути не довшою за 256 символів.')
      return
    }
    if (request.genitiveShortName && request.genitiveShortName.length > 50) {
      toast.showError('Коротка назва у родовому відмінку має бути не довшою за 50 символів.')
      return
    }

    if (panelMode === 'lookup-create') {
      runMutation.mutate(() => (
        activeTab === 'degrees' ? createAcademicDegree(request) : createTeacherPosition(request)
      ))
      return
    }

    if (!currentLookupItem) {
      return
    }

    runMutation.mutate(() => (
      activeTab === 'degrees'
        ? updateAcademicDegree(currentLookupItem.id, request)
        : updateTeacherPosition(currentLookupItem.id, request)
    ))
  }

  const submitSpecialty = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    const request = {
      code: specialtyForm.code.trim(),
      name: specialtyForm.name.trim(),
      isActive: specialtyForm.isActive,
    }

    if (panelMode === 'specialty-create') {
      runMutation.mutate(() => createSpecialty(request))
      return
    }

    if (selectedSpecialty) {
      runMutation.mutate(() => updateSpecialty(selectedSpecialty.id, request))
    }
  }

  const submitTeacher = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    const fullName = tidyText(teacherForm.fullName)

    if (!isValidFullName(fullName)) {
      toast.showError(fullNameValidationMessage)
      return
    }

    const shortName = makeTeacherShortName(fullName)
    const nameForms = cleanNameForms(normalizeNameForms(teacherForm.nameForms, fullName, shortName))
    if (!validateNameForms(nameForms, toast)) {
      return
    }

    const request: UpsertTeacherRequest = {
      fullName,
      shortName,
      nameForms,
      email: teacherForm.email.trim(),
      phoneNumber: teacherForm.phoneNumber.trim(),
      academicDegreeId: toEntityId(teacherForm.academicDegreeId),
      teacherPositionId: toEntityId(teacherForm.teacherPositionId),
      specialtyId: toEntityId(teacherForm.specialtyId),
      isActive: teacherForm.isActive,
    }

    if (panelMode === 'teacher-create') {
      runMutation.mutate(() => createTeacher(request))
      return
    }

    if (selectedTeacher) {
      runMutation.mutate(() => updateTeacher(selectedTeacher.id, request))
    }
  }

  const submitCommissionHead = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    const fullName = tidyText(commissionHeadForm.fullName)
    if (!isValidFullName(fullName)) {
      toast.showError(fullNameValidationMessage)
      return
    }

    const nameForms = cleanNameForms(normalizeNameForms(commissionHeadForm.nameForms, fullName))
    if (!validateNameForms(nameForms, toast)) {
      return
    }

    const request = {
      fullName,
      nameForms,
      position: tidyText(commissionHeadForm.position),
      company: tidyText(commissionHeadForm.company),
      specialty: tidyText(commissionHeadForm.specialty),
      secretaryEmail: secretary?.email ?? '',
    }

    if (!request.position || !request.company || !request.specialty) {
      toast.showError('Заповніть посаду, підприємство та спеціальність голови ДЕК.')
      return
    }
    if ([request.position, request.company, request.specialty].some((value) => value.length > 256)) {
      toast.showError('Посада, підприємство та спеціальність мають бути не довші за 256 символів.')
      return
    }

    if (panelMode === 'commission-head-create') {
      runMutation.mutate(() => createCommissionHead(request))
      return
    }

    if (selectedCommissionHead) {
      runMutation.mutate(() => updateCommissionHead(selectedCommissionHead.id, request))
    }
  }

  const submitSecretary = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    const fullName = tidyText(secretaryForm.fullName)

    if (!isValidFullName(fullName)) {
      toast.showError(fullNameValidationMessage)
      return
    }

    const request: UpsertSecretaryRequest = {
      email: secretaryForm.email.trim(),
      fullName,
      specialtyId: toEntityId(secretaryForm.specialtyId),
      isActive: secretaryForm.isActive,
      isSuperSecretary: secretaryForm.isSuperSecretary,
    }

    if (panelMode === 'secretary-create') {
      runMutation.mutate(() => createSecretary(request))
      return
    }

    if (selectedSecretary) {
      runMutation.mutate(() => updateSecretary(selectedSecretary.id, request))
    }
  }

  const confirmDeleteLookup = (item: AcademicDegreeDto | TeacherPositionDto) => {
    const label = activeTab === 'degrees' ? 'наукового ступеню' : 'посади'
    setConfirmState({
      title: activeTab === 'degrees' ? 'Видалення наукового ступеню' : 'Видалення посади',
      message: <>Ви впевнені, що хочете видалити {label} <strong>{lookupLabel(item)}</strong>?</>,
      confirmLabel: 'Видалити',
      onConfirm: () => runMutation.mutate(() => (
        activeTab === 'degrees' ? deleteAcademicDegree(item.id) : deleteTeacherPosition(item.id)
      )),
    })
  }

  const restoreLookup = (item: AcademicDegreeDto | TeacherPositionDto) => {
    runMutation.mutate(() => (
      activeTab === 'degrees' ? restoreAcademicDegree(item.id) : restoreTeacherPosition(item.id)
    ))
  }

  const confirmDeleteSpecialty = (specialty: SpecialtyDto) => {
    setConfirmState({
      title: 'Видалення спеціальності',
      message: <>Ви впевнені, що хочете видалити спеціальність <strong>{specialty.code}</strong>?</>,
      confirmLabel: 'Видалити',
      onConfirm: () => runMutation.mutate(() => deleteSpecialty(specialty.id)),
    })
  }

  const confirmDeleteTeacher = (teacher: TeacherDto) => {
    setConfirmState({
      title: 'Видалення викладача',
      message: <>Ви впевнені, що хочете видалити викладача <strong>{teacher.fullName}</strong>?</>,
      confirmLabel: 'Видалити',
      onConfirm: () => runMutation.mutate(() => deleteTeacher(teacher.id)),
    })
  }

  const confirmDeleteCommissionHead = (item: CommissionHeadDto) => {
    setConfirmState({
      title: 'Видалення голови ДЕК',
      message: <>Ви впевнені, що хочете видалити голову ДЕК <strong>{item.fullName}</strong>?</>,
      confirmLabel: 'Видалити',
      onConfirm: () => runMutation.mutate(() => deleteCommissionHead(item.id, secretary?.email ?? '')),
    })
  }

  const confirmDeleteSecretary = (item: SecretaryDto, hardDelete = false) => {
    setConfirmState({
      title: hardDelete ? 'Повне видалення секретаря' : 'Видалення секретаря',
      message: <>Ви впевнені, що хочете видалити секретаря <strong>{item.fullName}</strong>?</>,
      confirmLabel: 'Видалити',
      onConfirm: () => runMutation.mutate(() => (hardDelete ? hardDeleteSecretary(item.id) : deleteSecretary(item.id))),
    })
  }

  const title = tabs.find((tab) => tab.value === activeTab)?.label ?? ''
  const listItems: SidebarItem[] =
    activeTab === 'degrees'
      ? degrees
      : activeTab === 'positions'
        ? positions
        : activeTab === 'commission-heads'
          ? commissionHeads
          : specialties
  const isLoading =
    degreesQuery.isLoading ||
    positionsQuery.isLoading ||
    specialtiesQueryResult.isLoading ||
    commissionHeadsQueryResult.isLoading

  return (
    <div className="space-y-14">
      <div className="grid grid-cols-[410px_minmax(0,1fr)] gap-6">
        <aside className="space-y-6">
          <button
            type="button"
            onClick={beginCreate}
            className="h-16 w-full rounded-full bg-blue-600 px-8 text-3xl font-bold text-white shadow-sm transition hover:bg-blue-700 focus:outline-none focus:ring-4 focus:ring-blue-100"
          >
            + Додати {activeTab === 'degrees' ? 'ступінь' : activeTab === 'positions' ? 'посаду' : activeTab === 'commission-heads' ? 'голову ДЕК' : 'спеціальність'}
          </button>

          <section className="min-h-[520px] rounded-[22px] bg-slate-50/70 px-8 py-10 shadow-sm">
            <h2 className="text-xl font-extrabold uppercase tracking-wide text-slate-500">{title}</h2>
            <div className="mt-8 space-y-4">
              {isLoading && <p className="text-xl font-bold text-slate-400">Завантаження...</p>}
              {!isLoading && listItems.length === 0 && <p className="text-xl font-bold text-slate-400">Записів ще немає</p>}
              {listItems.map((item) => {
                const active =
                  activeTab === 'degrees'
                    ? idEquals(item.id, effectiveSelectedDegreeId)
                    : activeTab === 'positions'
                      ? idEquals(item.id, effectiveSelectedPositionId)
                      : activeTab === 'commission-heads'
                        ? idEquals(item.id, effectiveSelectedCommissionHeadId)
                        : idEquals(item.id, effectiveSelectedSpecialtyId)
                const label = activeTab === 'positions' && 'fullName' in item ? item.fullName : sidebarLabel(item)

                return (
                  <div key={String(item.id)} className="space-y-1">
                    <button
                      type="button"
                      onClick={() => {
                        if (activeTab === 'degrees') {
                          setSelectedDegreeId(item.id)
                          setPanelMode('details')
                        } else if (activeTab === 'positions') {
                          setSelectedPositionId(item.id)
                          setPanelMode('details')
                        } else if (activeTab === 'commission-heads') {
                          setSelectedCommissionHeadId(item.id)
                          setPanelMode('details')
                        } else {
                          beginSpecialtyEdit(item as SpecialtyDto)
                        }
                      }}
                      className={[
                        'flex min-h-16 w-full items-center justify-between gap-4 rounded-[18px] px-6 py-4 text-left text-2xl font-extrabold transition',
                        active && panelMode !== 'specialty-teachers' && panelMode !== 'specialty-secretaries'
                          ? 'bg-blue-600 text-white'
                          : activeTab === 'specialties' && active
                            ? 'border-2 border-blue-600 bg-white/65 text-blue-600'
                            : active
                              ? 'bg-blue-600 text-white'
                              : 'text-slate-500 hover:bg-white/80',
                      ].join(' ')}
                    >
                      <span className="min-w-0 whitespace-normal break-words leading-tight">{label}</span>
                      {isSidebarItemInactive(item) && <span className="text-sm font-bold uppercase opacity-75">архів</span>}
                    </button>

                    {activeTab === 'specialties' && active && (
                      <div className="ml-10 space-y-2 pt-1">
                        <button
                          type="button"
                          onClick={() => beginSpecialtyTeachers(item as SpecialtyDto)}
                          className={[
                            'h-14 w-full rounded-[18px] px-6 text-left text-2xl font-extrabold transition',
                            panelMode === 'specialty-teachers'
                              ? 'bg-blue-600 text-white'
                              : 'text-slate-500 hover:bg-white/80',
                          ].join(' ')}
                        >
                          Викладачі
                        </button>
                        <button
                          type="button"
                          onClick={() => beginSpecialtySecretaries(item as SpecialtyDto)}
                          className={[
                            'h-14 w-full rounded-[18px] px-6 text-left text-2xl font-extrabold transition',
                            panelMode === 'specialty-secretaries'
                              ? 'bg-blue-600 text-white'
                              : 'text-slate-500 hover:bg-white/80',
                          ].join(' ')}
                        >
                          Секретарі
                        </button>
                      </div>
                    )}
                  </div>
                )
              })}
            </div>
          </section>
        </aside>

        <section className="min-h-[620px] rounded-[22px] bg-slate-50/78 px-10 py-10 shadow-sm">
          {isLookupTab(activeTab) ? (
            renderLookupPanel()
          ) : activeTab === 'commission-heads' ? (
            renderCommissionHeadPanel()
          ) : (
            renderSpecialtyPanel()
          )}
        </section>
      </div>

      {confirmState && (
        <ConfirmDialog
          title={confirmState.title}
          confirmLabel={confirmState.confirmLabel}
          onConfirm={confirmState.onConfirm}
          onCancel={() => setConfirmState(null)}
        >
          <div className="space-y-6">
            <p>{confirmState.message}</p>
            <p className="font-extrabold">Цю дію неможливо скасувати</p>
          </div>
        </ConfirmDialog>
      )}
    </div>
  )

  function renderLookupPanel() {
    if (panelMode === 'lookup-create' || panelMode === 'lookup-edit') {
      return (
        <form onSubmit={submitLookup} className="flex h-full min-h-[540px] flex-col">
          <PanelHeader
            title={panelMode === 'lookup-create' ? `Додати ${activeTab === 'degrees' ? 'ступінь' : 'посаду'}` : `Змінити ${activeTab === 'degrees' ? 'ступінь' : 'посаду'}`}
            onClose={() => setPanelMode('details')}
          />
          <LookupFields value={lookupForm} onChange={setLookupForm} />
          <FormFooter>
            {panelMode === 'lookup-edit' && currentLookupItem && (
              <DangerButton onClick={() => confirmDeleteLookup(currentLookupItem)} />
            )}
            <SubmitButton label={panelMode === 'lookup-create' ? 'Додати' : 'Зберегти зміни'} />
          </FormFooter>
        </form>
      )
    }

    if (!currentLookupItem) {
      return <EmptyPanel title={activeTab === 'degrees' ? 'Наукові ступені' : 'Посади'} />
    }

    return (
      <div>
        <PanelHeader title={activeTab === 'positions' ? currentLookupItem.fullName : lookupLabel(currentLookupItem)}>
          {!currentLookupItem.isActive && (
            <ActionButton label="Поновити" icon={<RotateCcw size={22} />} onClick={() => restoreLookup(currentLookupItem)} />
          )}
          <ActionButton label="Видалити" tone="danger" icon={<Trash2 size={22} />} onClick={() => confirmDeleteLookup(currentLookupItem)} />
          <ActionButton label="Змінити" onClick={() => beginLookupEdit(currentLookupItem)} />
        </PanelHeader>
        <ReadonlyRows
          rows={[
            ['Повна назва', currentLookupItem.fullName],
            ['Коротка назва', currentLookupItem.shortName],
            ['Повна назва у родовому', currentLookupItem.genitiveFullName],
            ['Коротка назва у родовому', currentLookupItem.genitiveShortName],
            ['Стан', itemStatus(currentLookupItem.isActive)],
          ]}
          statusActive={currentLookupItem.isActive}
        />
      </div>
    )
  }

  function renderCommissionHeadPanel() {
    if (panelMode === 'commission-head-create' || panelMode === 'commission-head-edit') {
      return (
        <form onSubmit={submitCommissionHead} className="flex h-full min-h-[540px] flex-col">
          <PanelHeader
            title={panelMode === 'commission-head-create' ? 'Додати голову ДЕК' : selectedCommissionHead?.fullName || 'Змінити голову ДЕК'}
            onClose={() => setPanelMode('details')}
          />
          <CommissionHeadFields value={commissionHeadForm} onChange={setCommissionHeadForm} />
          <FormFooter>
            {panelMode === 'commission-head-edit' && selectedCommissionHead && (
              <DangerButton onClick={() => confirmDeleteCommissionHead(selectedCommissionHead)} />
            )}
            <SubmitButton label={panelMode === 'commission-head-create' ? 'Додати' : 'Зберегти зміни'} />
          </FormFooter>
        </form>
      )
    }

    if (!selectedCommissionHead) {
      return <EmptyPanel title="Голови ДЕК" />
    }

    return (
      <div>
        <PanelHeader title={selectedCommissionHead.fullName}>
          <ActionButton label="Видалити" tone="danger" icon={<Trash2 size={22} />} onClick={() => confirmDeleteCommissionHead(selectedCommissionHead)} />
          <ActionButton label="Змінити" onClick={() => beginCommissionHeadEdit(selectedCommissionHead)} />
        </PanelHeader>
        <ReadonlyRows
          rows={[
            ['ПІБ', selectedCommissionHead.fullName],
            ['Називний відмінок', selectedCommissionHead.nameForms.nominative],
            ['Родовий відмінок', selectedCommissionHead.nameForms.genitive],
            ['Давальний відмінок', selectedCommissionHead.nameForms.dative],
            ['Підпис', selectedCommissionHead.nameForms.signature],
            ['Посада', selectedCommissionHead.position],
            ['Підприємство', selectedCommissionHead.company],
            ['Спеціальність', selectedCommissionHead.specialty],
            ['Стан', selectedCommissionHead.isDeleted ? 'Архів' : 'Активний'],
          ]}
          statusActive={!selectedCommissionHead.isDeleted}
        />
      </div>
    )
  }

  function renderSpecialtyPanel() {
    if (panelMode === 'specialty-create' || panelMode === 'specialty-edit') {
      return (
        <form onSubmit={submitSpecialty} className="flex h-full min-h-[540px] flex-col">
          <PanelHeader title={panelMode === 'specialty-create' ? 'Додати спеціальність' : 'Змінити спеціальність'} onClose={() => setPanelMode('details')} />
          <SpecialtyFields value={specialtyForm} onChange={setSpecialtyForm} />
          <FormFooter>
            {panelMode === 'specialty-edit' && selectedSpecialty && (
              selectedSpecialty.isActive ? (
                <DangerButton onClick={() => confirmDeleteSpecialty(selectedSpecialty)} />
              ) : (
                <SecondaryButton label="Поновити" onClick={() => runMutation.mutate(() => restoreSpecialty(selectedSpecialty.id))} />
              )
            )}
            <SubmitButton label={panelMode === 'specialty-create' ? 'Додати' : 'Зберегти зміни'} />
          </FormFooter>
        </form>
      )
    }

    if (panelMode === 'teacher-create' || panelMode === 'teacher-edit') {
      return (
        <form onSubmit={submitTeacher} className="flex h-full min-h-[540px] flex-col">
          <PanelHeader title={panelMode === 'teacher-create' ? 'Додати викладача' : selectedTeacher?.shortName || 'Змінити викладача'} onClose={() => setPanelMode('details')} />
          <TeacherFields
            value={teacherForm}
            onChange={setTeacherForm}
            degrees={degrees}
            positions={positions}
            specialties={specialties}
          />
          <FormFooter>
            {panelMode === 'teacher-edit' && selectedTeacher && (
              selectedTeacher.isActive ? (
                <DangerButton onClick={() => confirmDeleteTeacher(selectedTeacher)} />
              ) : (
                <SecondaryButton label="Поновити" onClick={() => runMutation.mutate(() => restoreTeacher(selectedTeacher.id))} />
              )
            )}
            <SubmitButton label={panelMode === 'teacher-create' ? 'Додати' : 'Зберегти зміни'} />
          </FormFooter>
        </form>
      )
    }

    if (panelMode === 'secretary-create' || panelMode === 'secretary-edit') {
      return (
        <form onSubmit={submitSecretary} className="flex h-full min-h-[540px] flex-col">
          <PanelHeader title={panelMode === 'secretary-create' ? 'Додати секретаря' : selectedSecretary?.fullName || 'Змінити секретаря'} onClose={() => setPanelMode('details')} />
          <SecretaryFields value={secretaryForm} onChange={setSecretaryForm} specialties={specialties} />
          <FormFooter>
            {panelMode === 'secretary-edit' && selectedSecretary && (
              selectedSecretary.isActive ? (
                <DangerButton onClick={() => confirmDeleteSecretary(selectedSecretary)} />
              ) : (
                <div className="flex gap-3">
                  <SecondaryButton label="Поновити" onClick={() => runMutation.mutate(() => restoreSecretary(selectedSecretary.id))} />
                  <DangerButton label="Видалити назавжди" onClick={() => confirmDeleteSecretary(selectedSecretary, true)} />
                </div>
              )
            )}
            <SubmitButton label={panelMode === 'secretary-create' ? 'Додати' : 'Зберегти зміни'} />
          </FormFooter>
        </form>
      )
    }

    if (!selectedSpecialty) {
      return <EmptyPanel title="Спеціальності" />
    }

    if (panelMode === 'specialty-teachers') {
      return (
        <div>
          <PanelHeader title="Викладачі">
            <ActionButton label="Додати викладача" tone="success" icon={<UserPlus size={22} />} onClick={beginTeacherCreate} />
          </PanelHeader>
          <PeoplePreviewTable
            columns={['ПІБ', 'Короткий ПІБ', 'Науковий ступінь', 'Посада', 'Статус']}
            emptyText={teachersQueryResult.isLoading ? 'Завантаження...' : 'Викладачів ще немає'}
          >
            {teachers.map((teacher) => (
              <TeacherPreviewRow key={String(teacher.id)} teacher={teacher} onClick={() => beginTeacherEdit(teacher)} />
            ))}
          </PeoplePreviewTable>
        </div>
      )
    }

    if (panelMode === 'specialty-secretaries') {
      return (
        <div>
          <PanelHeader title="Секретарі">
            <ActionButton label="Додати секретаря" tone="success" icon={<ShieldCheck size={22} />} onClick={beginSecretaryCreate} />
          </PanelHeader>
          <PeoplePreviewTable
            columns={['ПІБ', 'Пошта', 'Роль', 'Статус']}
            emptyText="Секретарів ще немає"
            columnsClassName="grid-cols-[1.2fr_1.4fr_1fr_120px]"
          >
            {specialtySecretaries.map((item) => (
              <SecretaryPreviewRow key={String(item.id)} secretary={item} onClick={() => beginSecretaryEdit(item)} />
            ))}
          </PeoplePreviewTable>
        </div>
      )
    }

    return <EmptyPanel title={selectedSpecialty.code} />
  }
}

function PanelHeader({
  title,
  children,
  onClose,
}: {
  title: string
  children?: ReactNode
  onClose?: () => void
}) {
  return (
    <div className="mb-9 flex items-start justify-between gap-6">
      <h1 className="min-w-0 text-4xl font-extrabold uppercase text-blue-600">{title}</h1>
      <div className="flex flex-wrap justify-end gap-3">
        {children}
        {onClose && (
          <button
            type="button"
            aria-label="Закрити"
            onClick={onClose}
            className="grid size-12 place-items-center text-red-500 transition hover:text-red-600 focus:outline-none focus:ring-4 focus:ring-red-100"
          >
            <X size={42} strokeWidth={2.3} />
          </button>
        )}
      </div>
    </div>
  )
}

function ActionButton({
  label,
  icon,
  tone = 'primary',
  onClick,
}: {
  label: string
  icon?: ReactNode
  tone?: 'primary' | 'danger' | 'success'
  onClick: () => void
}) {
  const classes = {
    primary: 'border-blue-600 text-blue-600 hover:bg-blue-50',
    danger: 'border-red-500 text-red-500 hover:bg-red-50',
    success: 'border-green-500 text-green-600 hover:bg-green-50',
  }

  return (
    <button
      type="button"
      onClick={onClick}
      className={`flex h-11 items-center gap-2 rounded-full border-2 bg-white/40 px-6 text-xl font-extrabold transition focus:outline-none focus:ring-4 focus:ring-blue-100 ${classes[tone]}`}
    >
      {icon}
      {label}
    </button>
  )
}

function TextField({
  label,
  value,
  onChange,
  type = 'text',
  required = true,
  readOnly = false,
}: {
  label: string
  value: string
  onChange: (value: string) => void
  type?: string
  required?: boolean
  readOnly?: boolean
}) {
  return (
    <label className="grid grid-cols-[210px_minmax(0,1fr)] items-center gap-6 text-xl font-extrabold text-slate-600">
      <span>{label}</span>
      <input
        type={type}
        required={required}
        readOnly={readOnly}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className={`h-10 rounded-xl border px-4 outline-none ${readOnly ? 'bg-slate-100 text-slate-500' : ''}`}
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
    <label className="grid grid-cols-[210px_minmax(0,1fr)] items-center gap-6 text-xl font-extrabold text-slate-600">
      <span>{label}</span>
      <select
        required
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="h-10 rounded-xl border px-4 outline-none"
      >
        {children}
      </select>
    </label>
  )
}

function TeacherPositionSelectField({
  label,
  value,
  positions,
  onChange,
}: {
  label: string
  value: string
  positions: TeacherPositionDto[]
  onChange: (value: string) => void
}) {
  const [isOpen, setIsOpen] = useState(false)
  const selectedPosition = positions.find((position) => idEquals(position.id, value))

  return (
    <div className="grid grid-cols-[210px_minmax(0,1fr)] items-start gap-6 text-xl font-extrabold text-slate-600">
      <span className="pt-2">{label}</span>
      <div className="relative">
        <button
          type="button"
          onClick={() => setIsOpen((current) => !current)}
          className="min-h-10 w-full rounded-xl border bg-white px-4 py-2 text-left font-bold leading-snug text-slate-600 outline-none transition hover:bg-slate-50 focus:border-blue-500 focus:ring-4 focus:ring-blue-100"
        >
          {selectedPosition?.fullName || 'Оберіть посаду'}
        </button>
        {isOpen && (
          <div className="absolute z-20 mt-2 max-h-72 w-full overflow-y-auto rounded-xl border border-slate-200 bg-white py-2 shadow-lg">
            {positions.map((position) => (
              <button
                key={String(position.id)}
                type="button"
                onClick={() => {
                  onChange(String(position.id))
                  setIsOpen(false)
                }}
                className={[
                  'w-full px-4 py-3 text-left text-base font-bold leading-snug transition hover:bg-blue-50',
                  idEquals(position.id, value) ? 'bg-blue-600 text-white hover:bg-blue-600' : 'text-slate-600',
                ].join(' ')}
              >
                {position.fullName}
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

function CheckboxField({ label, checked, onChange }: { label: string; checked: boolean; onChange: (value: boolean) => void }) {
  return (
    <label className="flex items-center gap-3 text-lg font-extrabold text-slate-600">
      <input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} />
      {label}
    </label>
  )
}

function LookupFields({ value, onChange }: { value: LookupFormState; onChange: (value: LookupFormState) => void }) {
  return (
    <div className="max-w-[760px] space-y-5">
      <TextField label="Повна назва" value={value.fullName} onChange={(fullName) => onChange({ ...value, fullName })} />
      <TextField label="Коротка назва" value={value.shortName} onChange={(shortName) => onChange({ ...value, shortName })} />
      <TextField label="Повна назва у родовому" value={value.genitiveFullName} onChange={(genitiveFullName) => onChange({ ...value, genitiveFullName })} required={false} />
      <TextField label="Коротка назва у родовому" value={value.genitiveShortName} onChange={(genitiveShortName) => onChange({ ...value, genitiveShortName })} required={false} />
      <CheckboxField label="Активний запис" checked={value.isActive} onChange={(isActive) => onChange({ ...value, isActive })} />
    </div>
  )
}

function NameFormsFields({
  value,
  onChange,
}: {
  value: PersonNameFormsDto
  onChange: (value: PersonNameFormsDto) => void
}) {
  return (
    <div className="space-y-4 rounded-xl border border-slate-200 bg-white/45 p-5">
      <h2 className="text-sm font-extrabold uppercase text-slate-500">Форми ПІБ для документів</h2>
      {nameFormFields.map((field) => (
        <TextField
          key={field.key}
          label={field.label}
          value={value[field.key]}
          onChange={(nextValue) => onChange({ ...value, [field.key]: nextValue })}
          required={false}
        />
      ))}
    </div>
  )
}

function CommissionHeadFields({
  value,
  onChange,
}: {
  value: CommissionHeadFormState
  onChange: (value: CommissionHeadFormState) => void
}) {
  const handleFullNameChange = (fullName: string) => {
    const normalizedFullName = normalizeFullNameInput(fullName)
    const nextShortName = normalizedFullName

    onChange({
      ...value,
      fullName: normalizedFullName,
      nameForms: normalizeNameForms(value.nameForms, normalizedFullName, nextShortName),
    })
  }

  return (
    <div className="max-w-[800px] space-y-4">
      <TextField label="ПІБ" value={value.fullName} onChange={handleFullNameChange} />
      <NameFormsFields value={value.nameForms} onChange={(nameForms) => onChange({ ...value, nameForms })} />
      <TextField label="Посада" value={value.position} onChange={(position) => onChange({ ...value, position })} />
      <TextField label="Підприємство" value={value.company} onChange={(company) => onChange({ ...value, company })} />
      <TextField label="Спеціальність" value={value.specialty} onChange={(specialty) => onChange({ ...value, specialty })} />
    </div>
  )
}

function SpecialtyFields({ value, onChange }: { value: SpecialtyFormState; onChange: (value: SpecialtyFormState) => void }) {
  return (
    <div className="max-w-[760px] space-y-5">
      <TextField label="Код" value={value.code} onChange={(code) => onChange({ ...value, code })} />
      <TextField label="Назва спеціальності" value={value.name} onChange={(name) => onChange({ ...value, name })} />
      <CheckboxField label="Активний запис" checked={value.isActive} onChange={(isActive) => onChange({ ...value, isActive })} />
    </div>
  )
}

function TeacherFields({
  value,
  onChange,
  degrees,
  positions,
  specialties,
}: {
  value: TeacherFormState
  onChange: (value: TeacherFormState) => void
  degrees: AcademicDegreeDto[]
  positions: TeacherPositionDto[]
  specialties: SpecialtyDto[]
}) {
  const handleFullNameChange = (fullName: string) => {
    const normalizedFullName = normalizeFullNameInput(fullName)
    const shortName = makeTeacherShortName(normalizedFullName)

    onChange({
      ...value,
      fullName: normalizedFullName,
      shortName,
      nameForms: normalizeNameForms(value.nameForms, normalizedFullName, shortName),
    })
  }

  return (
    <div className="max-w-[800px] space-y-4">
      <TextField label="ПІБ" value={value.fullName} onChange={handleFullNameChange} />
      <TextField label="Короткий ПІБ" value={value.shortName} onChange={() => undefined} readOnly required={false} />
      <NameFormsFields value={value.nameForms} onChange={(nameForms) => onChange({ ...value, nameForms })} />
      <TextField label="Пошта" type="email" value={value.email} onChange={(email) => onChange({ ...value, email })} />
      <TextField label="Телефон" value={value.phoneNumber} onChange={(phoneNumber) => onChange({ ...value, phoneNumber })} />
      <SelectField label="Академічний рівень" value={value.academicDegreeId} onChange={(academicDegreeId) => onChange({ ...value, academicDegreeId })}>
        <option value="">Оберіть ступінь</option>
        {degrees.map((degree) => <option key={String(degree.id)} value={String(degree.id)}>{lookupLabel(degree)}</option>)}
      </SelectField>
      <TeacherPositionSelectField
        label="Посада"
        value={value.teacherPositionId}
        positions={positions}
        onChange={(teacherPositionId) => onChange({ ...value, teacherPositionId })}
      />
      <SelectField label="Спеціальність" value={value.specialtyId} onChange={(specialtyId) => onChange({ ...value, specialtyId })}>
        <option value="">Оберіть спеціальність</option>
        {specialties.map((specialty) => <option key={String(specialty.id)} value={String(specialty.id)}>{specialty.code} · {specialty.name}</option>)}
      </SelectField>
      <CheckboxField label="Активний запис" checked={value.isActive} onChange={(isActive) => onChange({ ...value, isActive })} />
    </div>
  )
}

function SecretaryFields({
  value,
  onChange,
  specialties,
}: {
  value: SecretaryFormState
  onChange: (value: SecretaryFormState) => void
  specialties: SpecialtyDto[]
}) {
  const handleFullNameChange = (fullName: string) => {
    onChange({ ...value, fullName: normalizeFullNameInput(fullName) })
  }

  return (
    <div className="max-w-[800px] space-y-4">
      <TextField label="ПІБ" value={value.fullName} onChange={handleFullNameChange} />
      <TextField label="Пошта" type="email" value={value.email} onChange={(email) => onChange({ ...value, email })} />
      <SelectField label="Спеціальність" value={value.specialtyId} onChange={(specialtyId) => onChange({ ...value, specialtyId })}>
        <option value="">Оберіть спеціальність</option>
        {specialties.map((specialty) => <option key={String(specialty.id)} value={String(specialty.id)}>{specialty.code} · {specialty.name}</option>)}
      </SelectField>
      <div className="flex flex-wrap gap-6">
        <CheckboxField label="Активний секретар" checked={value.isActive} onChange={(isActive) => onChange({ ...value, isActive })} />
        <CheckboxField label="Права супер-секретаря" checked={value.isSuperSecretary} onChange={(isSuperSecretary) => onChange({ ...value, isSuperSecretary })} />
      </div>
    </div>
  )
}

function ReadonlyRows({ rows, statusActive }: { rows: Array<[string, string]>; statusActive: boolean }) {
  return (
    <dl className="max-w-[760px] space-y-5">
      {rows.map(([label, value]) => (
        <div key={label} className="grid grid-cols-[210px_minmax(0,1fr)] items-center gap-6 text-xl font-extrabold">
          <dt className="text-slate-600">{label}</dt>
          <dd className={label === 'Стан' ? activeClass(statusActive) : 'text-slate-500'}>{value}</dd>
        </div>
      ))}
    </dl>
  )
}

function EmptyPanel({ title }: { title: string }) {
  return (
    <div className="grid min-h-[520px] place-items-center text-center">
      <div>
        <h1 className="text-4xl font-extrabold uppercase text-blue-600">{title}</h1>
        <p className="mt-4 text-2xl font-bold text-slate-400">Оберіть запис або створіть новий</p>
      </div>
    </div>
  )
}

function FormFooter({ children }: { children: ReactNode }) {
  return <div className="mt-auto flex flex-wrap items-end justify-end gap-4 pt-10">{children}</div>
}

function SubmitButton({ label }: { label: string }) {
  return (
    <button
      type="submit"
      className="h-12 rounded-full border-2 border-green-500 bg-white/35 px-8 text-2xl font-extrabold text-green-600 transition hover:bg-green-50 focus:outline-none focus:ring-4 focus:ring-green-100"
    >
      {label}
    </button>
  )
}

function DangerButton({ label = 'Видалити', onClick }: { label?: string; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="h-12 rounded-full border-2 border-red-500 bg-white/35 px-7 text-xl font-extrabold text-red-500 transition hover:bg-red-50 focus:outline-none focus:ring-4 focus:ring-red-100"
    >
      {label}
    </button>
  )
}

function SecondaryButton({ label, onClick }: { label: string; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="h-12 rounded-full border-2 border-slate-400 bg-white/35 px-7 text-xl font-extrabold text-slate-500 transition hover:bg-white focus:outline-none focus:ring-4 focus:ring-slate-100"
    >
      {label}
    </button>
  )
}

function PeoplePreviewTable({
  columns,
  children,
  emptyText,
  columnsClassName = 'grid-cols-[1.2fr_1fr_1fr_1fr_120px]',
}: {
  columns: string[]
  children: ReactNode
  emptyText: string
  columnsClassName?: string
}) {
  return (
    <div className="mt-20">
      <div className={`grid ${columnsClassName} border-b border-slate-300 px-5 pb-6 text-center text-base font-extrabold text-slate-500`}>
        {columns.map((column) => (
          <span key={column}>{column}</span>
        ))}
      </div>
      <div className="divide-y divide-slate-200">
        {children}
        {Array.isArray(children) && children.length === 0 && (
          <p className="px-5 py-8 text-center text-xl font-bold text-slate-400">{emptyText}</p>
        )}
      </div>
    </div>
  )
}

function TeacherPreviewRow({ teacher, onClick }: { teacher: TeacherDto; onClick: () => void }) {
  return (
    <PreviewRow columnsClassName="grid-cols-[1.2fr_1fr_1fr_1fr_120px]" onClick={onClick}>
      <span>{teacher.fullName}</span>
      <span>{teacher.shortName || '-'}</span>
      <span>{teacher.academicDegree || '-'}</span>
      <span>{teacher.teacherPosition || '-'}</span>
      <StatusCell isActive={teacher.isActive} />
    </PreviewRow>
  )
}

function SecretaryPreviewRow({ secretary, onClick }: { secretary: SecretaryDto; onClick: () => void }) {
  return (
    <PreviewRow columnsClassName="grid-cols-[1.2fr_1.4fr_1fr_120px]" onClick={onClick}>
      <span>{secretary.fullName}</span>
      <span>{secretary.email}</span>
      <span>{secretary.isSuperSecretary ? 'Супер-секретар' : 'Секретар'}</span>
      <StatusCell isActive={secretary.isActive} />
    </PreviewRow>
  )
}

function PreviewRow({
  children,
  columnsClassName,
  onClick,
}: {
  children: ReactNode
  columnsClassName: string
  onClick: () => void
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`grid min-h-20 w-full ${columnsClassName} items-center px-5 py-5 text-center text-base font-bold text-slate-500 transition hover:bg-white/70 focus:outline-none focus:ring-4 focus:ring-blue-100`}
    >
      {children}
    </button>
  )
}

function StatusCell({ isActive }: { isActive: boolean }) {
  return (
    <span className={`inline-flex items-center justify-center gap-2 font-extrabold ${activeClass(isActive)}`}>
      {isActive && <CheckCircle2 size={20} />}
      {itemStatus(isActive)}
    </span>
  )
}
