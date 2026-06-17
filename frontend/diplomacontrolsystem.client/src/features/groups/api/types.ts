export type EntityId = number | string
export type EducationLevel = 'Bachelor' | 'Master'

export interface PersonNameFormsDto {
  nominative: string
  genitive: string
  dative: string
  signature: string
}

export interface GroupDto {
  id: EntityId
  name: string
}

export interface AcademicYearOverviewResponse {
  year: string
  defenseYear: string
  groups: GroupDto[]
}

export interface PhysicalChecklistDto {
  hasStudentCard: boolean
  hasGradeBook: boolean
  hasCircular: boolean
  hasSignedReview: boolean
  hasCopyOfBankReceipt: boolean
  hasExplanatoryNote: boolean
}

export interface ElectronicChecklistDto {
  hasRegulatoryControl: boolean
  hasExplanatoryNoteDoc: boolean
  hasExplanatoryNotePdf: boolean
  hasPlagiarismReportPdf: boolean
  hasReviewDoc: boolean
  hasPresentationPpt: boolean
}

export interface GroupStudentResponse {
  id: EntityId
  fullName: string
  nameForms: PersonNameFormsDto
  supervisorName: string | null
  physicalChecklist: PhysicalChecklistDto | null
  electronicChecklist: ElectronicChecklistDto | null
}

export interface CreateGroupRequest {
  secretaryEmail: string
  name: string
  year: string
  educationLevel: EducationLevel
  studentsFile?: File | null
  googleDriveUrl?: string | null
}

export interface ImportGroupDefenceResultsRequest {
  secretaryEmail: string
  resultsFile?: File | null
  googleDriveLink?: string | null
}

export interface CreateGroupResponse {
  groupId: EntityId
  name: string
  year: string
  defenseYear: string
  educationLevel: string
  studentsCreated: EntityId
  importStatistics: StudentImportStatisticsDto
}

export interface StudentImportStatisticsDto {
  supervisorsMatched: EntityId
  supervisorsMissing: EntityId
  supervisorsUnspecified: EntityId
  topicsImported: EntityId
  practiceBasesImported: EntityId
}

export interface ImportTableColumnDto {
  key: string
  displayName: string
  required: boolean
  acceptedHeaders: string[]
}

export interface ImportTableColumnsResponse {
  columns: ImportTableColumnDto[]
}

export interface ImportGroupDefenceResultsResponse {
  groupId: EntityId
  groupName: string
  rowsRead: EntityId
  studentsUpdated: EntityId
  plagiarismImported: EntityId
  scoresImported: EntityId
  defenceDatesImported: EntityId
}

export interface UpdateGroupRequest {
  secretaryEmail: string
  name: string | null
  year: string | null
  educationLevel: EducationLevel | null
}

export interface UpdateGroupResponse {
  id: EntityId
  name: string
  year: string
  defenseYear: string
  educationLevel: string
}

export interface AddStudentRequest {
  secretaryEmail: string
  lastName: string
  firstName: string
  middleName: string
}

export interface AddStudentResponse {
  studentId: EntityId
  fullName: string
  groupId: EntityId
}

export type StatisticSectionKey =
  | 'gradesAndRecommendations'
  | 'workCharacter'
  | 'complexDiplomaDesign'
  | 'additional'
  | 'performanceIndicators'

export type StatisticItemKey =
  | 'excellent'
  | 'good'
  | 'satisfactory'
  | 'diplomaWithHonors'
  | 'recommendedForMaster'
  | 'researchBased'
  | 'realProjects'
  | 'ecoFriendly'
  | 'enterpriseOrdered'
  | 'interuniversity'
  | 'interdepartmental'
  | 'departmental'
  | 'complexProjectParticipant'
  | 'recommendedForImplementation'
  | 'defendedAtEnterprise'
  | 'educationQuality'
  | 'overallSuccess'

export interface StatisticItemDto {
  key: StatisticItemKey
  count: EntityId
  percentage: number | string
}

export interface StatisticSectionDto {
  key: StatisticSectionKey
  items: StatisticItemDto[]
}

export interface StatisticsSnapshotDto {
  defenseYear: string
  groupsCount: EntityId
  totalStudents: EntityId
  sections: StatisticSectionDto[]
}

export interface GroupStatisticsResponse {
  groupId: EntityId
  groupName: string
  totalStudents: EntityId
  sections: StatisticSectionDto[]
}

export interface PreviousYearComparisonResponse {
  groupId: EntityId
  groupName: string
  currentGroup: StatisticsSnapshotDto
  previousYear: StatisticsSnapshotDto | null
}

export interface SupervisorWorkloadResponse {
  groupId: EntityId
  groupName: string
  summary: {
    totalSupervisors: EntityId
    totalStudents: EntityId
  }
  items: SupervisorWorkloadItemDto[]
}

export interface SupervisorWorkloadItemDto {
  key: 'supervisor' | 'withoutSupervisor'
  teacherId: EntityId | null
  fullName: string | null
  shortName: string | null
  studentsCount: EntityId
  averageScore: number | null
  diplomasWithHonorsCount: EntityId
  averagePlagiarismPercent: number | null
}

export interface PracticeBaseRatingResponse {
  groupId: EntityId
  groupName: string
  totalStudents: EntityId
  totalPracticeBases: EntityId
  items: PracticeBaseRatingItemDto[]
}

export interface PracticeBaseRatingItemDto {
  key: 'practiceBase' | 'withoutPracticeBase'
  rank: EntityId | null
  practiceBase: string | null
  studentsCount: EntityId
}

export interface StudentNameDto {
  lastName: string
  firstName: string
  middleName: string
}

export interface QualificationWorkDto {
  topic: string
  supervisorId: EntityId | null
  supervisorName: string | null
  practiceBase: string
  reviewerId: EntityId | null
  reviewerName: string | null
  defenceQuestions: DefenceQuestionDto[]
  defenceQuestionAuthorOptions?: DefenceQuestionAuthorOptionDto[]
}

export interface DefenceQuestionDto {
  askedBy: string
  text: string
}

export interface DefenceQuestionAuthorOptionDto {
  shortName: string
  fullName: string
  role: string
}

export interface DefenceInfoDto {
  defenceDate: string | null
  protocolNumber: number | null
  durationOfDefenceMinutes: number | null
  presentationSheets: number | null
  workSheets: number | null
}

export type EctsGrade = 'None' | 'A' | 'B' | 'C' | 'D' | 'E'
export type NationalGrade = 'None' | 'Excellent' | 'Good' | 'Satisfactory'

export interface DefenceResultsDto {
  plagiarismPercent: number
  uniquePercent: number
  commissionScore: number
  ectsGrade: EctsGrade
  nationalGrade: NationalGrade
  hasDiplomaWithHonors: boolean
}

export interface CharacteristicsDto {
  isResearchBased: boolean
  hasRealProjects: boolean
  isEcoFriendly: boolean
  isEnterpriseOrdered: boolean
  isComplexInteruniversity: boolean
  isComplexInterdepartmental: boolean
  isComplexDepartmental: boolean
  isComplexProjectParticipant: boolean
  isRecommendedForMaster: boolean
  isRecommendedForImplementation: boolean
  isDefendedAtEnterprise: boolean
}

export interface StudentDetailsResponse {
  id: EntityId
  groupId: EntityId
  fullName: string
  name: StudentNameDto
  nameForms: PersonNameFormsDto
  qualificationWork: QualificationWorkDto | null
  physicalChecklist: PhysicalChecklistDto | null
  electronicChecklist: ElectronicChecklistDto | null
  defenceInfo: DefenceInfoDto | null
  defenceResults: DefenceResultsDto | null
  characteristics: CharacteristicsDto | null
}

export interface TeacherOptionDto {
  id: EntityId
  fullName: string
  shortName: string
}

export interface QualificationWorkOptionsResponse {
  teachers: TeacherOptionDto[]
  supervisors: TeacherOptionDto[]
  reviewers: TeacherOptionDto[]
  defenceQuestionAuthors?: DefenceQuestionAuthorOptionDto[]
}

export interface UpdateStudentNameRequest extends AddStudentRequest {
  nameForms?: PersonNameFormsDto | null
}

export interface UpdateStudentQualificationWorkRequest {
  secretaryEmail: string
  topic: string
  supervisorId: EntityId | null
  practiceBase: string
  reviewerId: EntityId | null
}

export interface UpdateStudentDefenceRequest {
  secretaryEmail: string
  defenceDate: string | null
  protocolNumber: number | null
  durationOfDefenceMinutes: number | null
  presentationSheets: number | null
  workSheets: number | null
}

export type UpdatePhysicalChecklistRequest = PhysicalChecklistDto & { secretaryEmail: string }
export type UpdateElectronicChecklistRequest = ElectronicChecklistDto & { secretaryEmail: string }
export type UpdateDefenceResultsRequest = DefenceResultsDto & { secretaryEmail: string }
export type UpdateQualificationWorkCharacteristicsRequest = CharacteristicsDto & { secretaryEmail: string }
export type UpdateDefenceQuestionsRequest = { secretaryEmail: string; questions: DefenceQuestionDto[] }
