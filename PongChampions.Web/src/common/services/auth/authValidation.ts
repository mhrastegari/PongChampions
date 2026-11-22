import type { IRegistration } from '@models/auth';
import { constants } from '@shared/constants';
import { vocabService } from '@services/vocab';

const { format } = vocabService;
const rules = constants.validation;

export type RegistrationField = keyof IRegistration | 'confirmPassword';

export type RegistrationErrors = Partial<Record<RegistrationField, string>>;

// Mirrors the API's UserRegisterDto rules so most mistakes never reach the server.
export function validateRegistration(
  values: IRegistration & { confirmPassword: string }
): RegistrationErrors {
  const errors: RegistrationErrors = {};
  const username = values.username.trim();
  const displayName = values.displayName.trim();
  const passwordBytes = new TextEncoder().encode(values.password).length;

  if (username.length < rules.usernameMin || username.length > rules.usernameMax)
    errors.username = format(
      'validation_username_length',
      rules.usernameMin,
      rules.usernameMax
    );
  else if (!rules.usernamePattern.test(username))
    errors.username = vocabService.get('validation_username_characters');

  if (!displayName)
    errors.displayName = vocabService.get('validation_display_name_required');
  else if (displayName.length > rules.displayNameMax)
    errors.displayName = format(
      'validation_display_name_length',
      rules.displayNameMax
    );

  if (values.password.length < rules.passwordMin)
    errors.password = format('validation_password_min', rules.passwordMin);
  else if (
    values.password.length > rules.passwordMax ||
    passwordBytes > rules.passwordMaxBytes
  )
    errors.password = format('validation_password_max', rules.passwordMaxBytes);

  if (values.confirmPassword !== values.password)
    errors.confirmPassword = vocabService.get('validation_password_mismatch');

  if ((values.country ?? '').trim().length > rules.countryMax)
    errors.country = format('validation_country_length', rules.countryMax);

  return errors;
}
