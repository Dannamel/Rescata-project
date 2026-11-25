import { statusName, statusNames } from './format.js'

export default function StatusBadge({ status }) {
  const variant = status in statusNames ? status.toLowerCase() : 'unknown'
  return <span className={'status status-' + variant}>{statusName(status)}</span>
}
