import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CheckCircle2, RotateCcw, ShieldCheck, Trash2, UserPlus, X } from 'lucide-react'
import { useMemo, useState, type FormEvent, type ReactNode } from 'react'
import { Navigate, useSearchParams } from 'react-router-dom'
import { useAuth } from '../../features/auth/model/useAuth'
import type { EntityId } from '../../features/groups/api/types'
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

type CatalogTab = 'degrees' | 'positions' | 'specialties'
type PanelMode =
  | 'details'
  | 'lookup-create'
  | 'lookup-edit'
  | 'specialty-create'
  | 'specialty-edit'
  | 'teacher-create'
  | 'teacher-edit'
  | 'secretary-create'
  | 'secretary-edit'

interface LookupFormState {
  fullName: string
  shortName: string
  isActive: boolean
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

const tabs: Array<{ value: CatalogTab; label: string }> = [
  { value: 'degrees', label: 'Ступені' },
  { value: 'positions', label: 'Посади' },
  { value: 'specialties', label: 'Спеціальності' },
]

const emptyLookupForm: LookupFormState = {
  fullName: '',
  shortName: '',
  isActive: true,
}

const emptySpecialtyForm: SpecialtyFormState = {
  code: '',
  name: '',
  isActive: true,
}

function idEquals(left: EntityId, right: EntityId | string | undefined) {
  return String(left) === String(right ?? '')
}

function lookupLabel(item: AcademicDegreeDto | TeacherPositionDto) {
  return item.shortName || item.fullName
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
  return {
    fullName: teacher?.fullName ?? '',
    shortName: teacher?.shortName ?? '',
    email: teacher?.email ?? '',
    phoneNumber: teacher?.phoneNumber ?? '',
    academicDegreeId: String(teacher?.academicDegreeId ?? degreeId ?? ''),
    teacherPositionId: String(teacher?.teacherPositionId ?? positionId ?? ''),
    specialtyId: String(teacher?.specialtyId ?? specialtyId ?? ''),
    isActive: teacher?.isActive ?? true,
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
  return value === 'degrees' || value === 'positions' || value === 'specialties' ? value : 'specialties'
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
  const [lookupForm, setLookupForm] = useState<LookupFormState>(emptyLookupForm)
  const [specialtyForm, setSpecialtyForm] = useState<SpecialtyFormState>(emptySpecialtyForm)
  const [teacherForm, setTeacherForm] = useState<TeacherFormState>(() => makeTeacherForm(null, undefined, undefined, undefined))
  const [secretaryForm, setSecretaryForm] = useState<SecretaryFormState>(() => makeSecretaryForm(null, undefined))
  const [confirmState, setConfirmState] = useState<ConfirmState | null>(null)

  const degreesQuery = useQuery(academicDegreesQuery())
  const positionsQuery = useQuery(teacherPositionsQuery())
  const specialtiesQueryResult = useQuery(specialtiesQuery())
  const secretariesQueryResult = useQuery(secretariesQuery())

  const degrees = useMemo(() => degreesQuery.data ?? [], [degreesQuery.data])
  const positions = useMemo(() => positionsQuery.data ?? [], [positionsQuery.data])
  const specialties = useMemo(() => specialtiesQueryResult.data ?? [], [specialtiesQueryResult.data])
  const secretaries = useMemo(() => secretariesQueryResult.data ?? [], [secretariesQueryResult.data])
  const effectiveSelectedDegreeId = selectedDegreeId ?? degrees[0]?.id
  const effectiveSelectedPositionId = selectedPositionId ?? positions[0]?.id
  const effectiveSelectedSpecialtyId = selectedSpecialtyId ?? specialties[0]?.id
  const teachersQueryResult = useQuery(teachersQuery(effectiveSelectedSpecialtyId))
  const teachers = useMemo(() => teachersQueryResult.data ?? [], [teachersQueryResult.data])
  const selectedDegree = degrees.find((degree) => idEquals(degree.id, effectiveSelectedDegreeId))
  const selectedPosition = positions.find((position) => idEquals(position.id, effectiveSelectedPositionId))
  const selectedSpecialty = specialties.find((specialty) => idEquals(specialty.id, effectiveSelectedSpecialtyId))
  const selectedTeacher = teachers.find((teacher) => idEquals(teacher.id, selectedTeacherId))
  const selectedSecretary = secretaries.find((item) => idEquals(item.id, selectedSecretaryId))
  const currentLookupItem = activeTab === 'degrees' ? selectedDegree : selectedPosition
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

    setSpecialtyForm(emptySpecialtyForm)
    setPanelMode('specialty-create')
  }

  const beginLookupEdit = (item: AcademicDegreeDto | TeacherPositionDto) => {
    setLookupForm({
      fullName: item.fullName,
      shortName: item.shortName,
      isActive: item.isActive,
    })
    setPanelMode('lookup-edit')
  }

  const beginSpecialtyEdit = (specialty: SpecialtyDto) => {
    setSpecialtyForm({
      code: specialty.code,
      name: specialty.name,
      isActive: specialty.isActive,
    })
    setPanelMode('specialty-edit')
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
      isActive: lookupForm.isActive,
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

    const request: UpsertTeacherRequest = {
      fullName: teacherForm.fullName.trim(),
      shortName: teacherForm.shortName.trim(),
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

  const submitSecretary = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    const request: UpsertSecretaryRequest = {
      email: secretaryForm.email.trim(),
      fullName: secretaryForm.fullName.trim(),
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

  const confirmDeleteSecretary = (item: SecretaryDto, hardDelete = false) => {
    setConfirmState({
      title: hardDelete ? 'Повне видалення секретаря' : 'Видалення секретаря',
      message: <>Ви впевнені, що хочете видалити секретаря <strong>{item.fullName}</strong>?</>,
      confirmLabel: 'Видалити',
      onConfirm: () => runMutation.mutate(() => (hardDelete ? hardDeleteSecretary(item.id) : deleteSecretary(item.id))),
    })
  }

  const title = tabs.find((tab) => tab.value === activeTab)?.label ?? ''
  const listItems = activeTab === 'degrees' ? degrees : activeTab === 'positions' ? positions : specialties
  const isLoading = degreesQuery.isLoading || positionsQuery.isLoading || specialtiesQueryResult.isLoading

  return (
    <div className="space-y-14">
      <div className="grid grid-cols-[410px_minmax(0,1fr)] gap-6">
        <aside className="space-y-6">
          <button
            type="button"
            onClick={beginCreate}
            className="h-16 w-full rounded-full bg-blue-600 px-8 text-3xl font-bold text-white shadow-sm transition hover:bg-blue-700 focus:outline-none focus:ring-4 focus:ring-blue-100"
          >
            + Додати {activeTab === 'degrees' ? 'ступінь' : activeTab === 'positions' ? 'посаду' : 'спеціальність'}
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
                      : idEquals(item.id, effectiveSelectedSpecialtyId)
                const label = 'shortName' in item ? lookupLabel(item) : item.code

                return (
                  <button
                    key={String(item.id)}
                    type="button"
                    onClick={() => {
                      if (activeTab === 'degrees') {
                        setSelectedDegreeId(item.id)
                      } else if (activeTab === 'positions') {
                        setSelectedPositionId(item.id)
                      } else {
                        setSelectedSpecialtyId(item.id)
                      }
                      setPanelMode('details')
                    }}
                    className={[
                      'flex h-16 w-full items-center justify-between rounded-[18px] px-6 text-left text-2xl font-extrabold transition',
                      active ? 'bg-blue-600 text-white' : 'text-slate-500 hover:bg-white/80',
                    ].join(' ')}
                  >
                    <span>{label}</span>
                    {!item.isActive && <span className="text-sm font-bold uppercase opacity-75">архів</span>}
                  </button>
                )
              })}
            </div>
          </section>
        </aside>

        <section className="min-h-[620px] rounded-[22px] bg-slate-50/78 px-10 py-10 shadow-sm">
          {isLookupTab(activeTab) ? (
            renderLookupPanel()
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
        <PanelHeader title={lookupLabel(currentLookupItem)}>
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
            ['Стан', itemStatus(currentLookupItem.isActive)],
          ]}
          statusActive={currentLookupItem.isActive}
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
              <DangerButton onClick={() => confirmDeleteSpecialty(selectedSpecialty)} />
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

    return (
      <div>
        <PanelHeader title="Викладачі">
          {!selectedSpecialty.isActive && (
            <ActionButton label="Поновити" icon={<RotateCcw size={22} />} onClick={() => runMutation.mutate(() => restoreSpecialty(selectedSpecialty.id))} />
          )}
          <ActionButton label="Змінити спеціальність" onClick={() => beginSpecialtyEdit(selectedSpecialty)} />
          <ActionButton label="Додати секретаря" tone="success" icon={<ShieldCheck size={22} />} onClick={beginSecretaryCreate} />
          <ActionButton label="Додати викладача" tone="success" icon={<UserPlus size={22} />} onClick={beginTeacherCreate} />
        </PanelHeader>

        <div className="mt-4 flex items-center justify-between rounded-full bg-white/70 px-6 py-3">
          <div>
            <p className="text-xl font-extrabold text-blue-600">{selectedSpecialty.code}</p>
            <p className="text-sm font-bold text-slate-500">{selectedSpecialty.name}</p>
          </div>
          <span className={`text-base font-extrabold ${activeClass(selectedSpecialty.isActive)}`}>
            {itemStatus(selectedSpecialty.isActive)}
          </span>
        </div>

        <div className="mt-9 grid grid-cols-2 gap-8">
          <PeopleList
            title="Викладачі"
            emptyText={teachersQueryResult.isLoading ? 'Завантаження...' : 'Викладачів ще немає'}
          >
            {teachers.map((teacher) => (
              <PersonButton
                key={String(teacher.id)}
                title={teacher.shortName || teacher.fullName}
                subtitle={`${teacher.academicDegree}, ${teacher.teacherPosition}`}
                isActive={teacher.isActive}
                onClick={() => beginTeacherEdit(teacher)}
              />
            ))}
          </PeopleList>

          <PeopleList title="Секретарі" emptyText="Секретарів ще немає">
            {specialtySecretaries.map((item) => (
              <PersonButton
                key={String(item.id)}
                title={item.fullName}
                subtitle={`${item.email}${item.isSuperSecretary ? ' · супер-секретар' : ''}`}
                isActive={item.isActive}
                onClick={() => beginSecretaryEdit(item)}
              />
            ))}
          </PeopleList>
        </div>
      </div>
    )
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
}: {
  label: string
  value: string
  onChange: (value: string) => void
  type?: string
  required?: boolean
}) {
  return (
    <label className="grid grid-cols-[210px_minmax(0,1fr)] items-center gap-6 text-xl font-extrabold text-slate-600">
      <span>{label}</span>
      <input
        type={type}
        required={required}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="h-10 rounded-xl border px-4 outline-none"
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
      <CheckboxField label="Активний запис" checked={value.isActive} onChange={(isActive) => onChange({ ...value, isActive })} />
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
  return (
    <div className="max-w-[800px] space-y-4">
      <TextField label="ПІБ" value={value.fullName} onChange={(fullName) => onChange({ ...value, fullName })} />
      <TextField label="Короткий ПІБ" value={value.shortName} onChange={(shortName) => onChange({ ...value, shortName })} />
      <TextField label="Пошта" type="email" value={value.email} onChange={(email) => onChange({ ...value, email })} />
      <TextField label="Телефон" value={value.phoneNumber} onChange={(phoneNumber) => onChange({ ...value, phoneNumber })} />
      <SelectField label="Академічний рівень" value={value.academicDegreeId} onChange={(academicDegreeId) => onChange({ ...value, academicDegreeId })}>
        <option value="">Оберіть ступінь</option>
        {degrees.map((degree) => <option key={String(degree.id)} value={String(degree.id)}>{lookupLabel(degree)}</option>)}
      </SelectField>
      <SelectField label="Посада" value={value.teacherPositionId} onChange={(teacherPositionId) => onChange({ ...value, teacherPositionId })}>
        <option value="">Оберіть посаду</option>
        {positions.map((position) => <option key={String(position.id)} value={String(position.id)}>{lookupLabel(position)}</option>)}
      </SelectField>
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
  return (
    <div className="max-w-[800px] space-y-4">
      <TextField label="ПІБ" value={value.fullName} onChange={(fullName) => onChange({ ...value, fullName })} />
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

function PeopleList({ title, emptyText, children }: { title: string; emptyText: string; children: ReactNode }) {
  return (
    <section>
      <h2 className="text-2xl font-extrabold text-blue-600">{title}</h2>
      <div className="mt-5 space-y-3">
        {children}
        {Array.isArray(children) && children.length === 0 && (
          <p className="rounded-[18px] bg-white/55 px-6 py-5 text-xl font-bold text-slate-400">{emptyText}</p>
        )}
      </div>
    </section>
  )
}

function PersonButton({
  title,
  subtitle,
  isActive,
  onClick,
}: {
  title: string
  subtitle: string
  isActive: boolean
  onClick: () => void
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="flex min-h-16 w-full items-center justify-between rounded-[18px] px-6 py-4 text-left transition hover:bg-white/80"
    >
      <span>
        <span className="block text-2xl font-extrabold text-slate-500">{title}</span>
        <span className="mt-1 block text-sm font-bold text-slate-400">{subtitle}</span>
      </span>
      {isActive ? <CheckCircle2 className="text-green-500" size={24} /> : <span className="text-sm font-bold uppercase text-slate-400">архів</span>}
    </button>
  )
}
