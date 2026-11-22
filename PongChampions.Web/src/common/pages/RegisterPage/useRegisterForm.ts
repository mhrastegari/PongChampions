import { useState, type FormEvent } from 'react';
import type { IRegistration } from '@models/auth';
import { ConflictError, getErrorMessage } from '@services/_api';
import {
  authService,
  validateRegistration,
  type RegistrationErrors,
  type RegistrationField,
} from '@services/auth';

type RegistrationValues = Required<IRegistration> & { confirmPassword: string };

const emptyValues: RegistrationValues = {
  username: '',
  displayName: '',
  password: '',
  confirmPassword: '',
  country: '',
};

export function useRegisterForm() {
  const [values, setValues] = useState(emptyValues);
  const [errors, setErrors] = useState<RegistrationErrors>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  function setField(field: RegistrationField, value: string) {
    setValues((current) => ({ ...current, [field]: value }));
    setErrors((current) => ({ ...current, [field]: undefined }));
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault();

    const validation = validateRegistration(values);

    setErrors(validation);
    setFormError(null);

    if (Object.values(validation).some(Boolean)) return;

    setPending(true);

    try {
      // Registration signs the user in; the page redirects when it sees the session.
      await authService.register(values);
    } catch (failure) {
      if (failure instanceof ConflictError)
        setErrors({ username: getErrorMessage(failure) });
      else setFormError(getErrorMessage(failure));

      setPending(false);
    }
  }

  return { values, setField, errors, formError, pending, onSubmit };
}
