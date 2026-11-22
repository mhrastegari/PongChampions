import { useEffect, useRef, useState } from 'react';
import { Button, type ButtonVariant } from '@components/Button';

interface IConfirmButtonProps {
  label: string;
  prompt: string;
  confirmLabel: string;
  cancelLabel: string;
  onConfirm: () => void;
  variant?: ButtonVariant;
  loading?: boolean;
  disabled?: boolean;
}

// A two-step inline confirmation for destructive actions; no modal needed.
export function ConfirmButton({
  label,
  prompt,
  confirmLabel,
  cancelLabel,
  onConfirm,
  variant = 'secondary',
  loading = false,
  disabled = false,
}: IConfirmButtonProps) {
  const [confirming, setConfirming] = useState(false);
  const confirmRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (confirming) confirmRef.current?.focus();
  }, [confirming]);

  if (!confirming)
    return (
      <Button
        variant={variant}
        disabled={disabled}
        onClick={() => setConfirming(true)}
      >
        {label}
      </Button>
    );

  return (
    <div
      className="flex flex-wrap items-center gap-2"
      role="group"
      aria-label={prompt}
    >
      <span className="text-sm text-muted-foreground">{prompt}</span>
      <Button
        ref={confirmRef}
        variant="danger"
        loading={loading}
        disabled={disabled}
        onClick={onConfirm}
      >
        {confirmLabel}
      </Button>
      <Button
        variant="ghost"
        disabled={loading}
        onClick={() => setConfirming(false)}
      >
        {cancelLabel}
      </Button>
    </div>
  );
}
