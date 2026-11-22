import { Link, useSearch } from '@tanstack/react-router';
import { Alert } from '@components/Alert';
import { Button } from '@components/Button';
import { TextField } from '@components/TextField';
import { useDocumentTitle } from '@hooks/useDocumentTitle';
import { useRedirectWhenSignedIn } from '@hooks/useRedirectWhenSignedIn';
import { constants } from '@shared/constants';
import { vocabService } from '@services/vocab';
import { AuthPanel } from '@widgets/AuthPanel';
import { useRegisterForm } from './useRegisterForm';

const { format, get: vocab } = vocabService;
const rules = constants.validation;

export function RegisterPage() {
  const { redirect } = useSearch({ from: '/register' });
  const { values, setField, errors, formError, pending, onSubmit } =
    useRegisterForm();

  useDocumentTitle(vocab('register_title'));
  useRedirectWhenSignedIn(redirect ?? '/');

  return (
    <AuthPanel
      title={vocab('register_title')}
      description={vocab('register_description')}
      footer={
        <>
          {vocab('register_have_account')}{' '}
          <Link
            to="/login"
            search={{ redirect }}
            className="font-semibold text-primary underline-offset-4 hover:underline"
          >
            {vocab('login')}
          </Link>
        </>
      }
    >
      <form className="flex flex-col gap-4" onSubmit={onSubmit} noValidate>
        <TextField
          label={vocab('field_username')}
          value={values.username}
          onChange={(event) => setField('username', event.target.value)}
          error={errors.username}
          hint={format('hint_username', rules.usernameMin, rules.usernameMax)}
          autoComplete="username"
          autoCapitalize="none"
          spellCheck={false}
          maxLength={rules.usernameMax}
          required
        />
        <TextField
          label={vocab('field_display_name')}
          value={values.displayName}
          onChange={(event) => setField('displayName', event.target.value)}
          error={errors.displayName}
          hint={vocab('hint_display_name')}
          autoComplete="nickname"
          maxLength={rules.displayNameMax}
          required
        />
        <TextField
          label={vocab('field_password')}
          type="password"
          value={values.password}
          onChange={(event) => setField('password', event.target.value)}
          error={errors.password}
          hint={format('hint_password', rules.passwordMin, rules.passwordMax)}
          autoComplete="new-password"
          required
        />
        <TextField
          label={vocab('field_confirm_password')}
          type="password"
          value={values.confirmPassword}
          onChange={(event) => setField('confirmPassword', event.target.value)}
          error={errors.confirmPassword}
          autoComplete="new-password"
          required
        />
        <TextField
          label={vocab('field_country')}
          value={values.country}
          onChange={(event) => setField('country', event.target.value)}
          error={errors.country}
          autoComplete="country-name"
          maxLength={rules.countryMax}
        />
        {formError && <Alert>{formError}</Alert>}
        <Button type="submit" size="lg" loading={pending}>
          {vocab('register_submit')}
        </Button>
      </form>
    </AuthPanel>
  );
}
