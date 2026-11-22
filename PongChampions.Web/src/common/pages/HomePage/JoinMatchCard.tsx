import { Button } from '@components/Button';
import { Card } from '@components/Card';
import { TextField } from '@components/TextField';
import { useAuth } from '@hooks/useAuth';
import { constants } from '@shared/constants';
import { vocabService } from '@services/vocab';
import { useJoinMatch } from './useJoinMatch';

const vocab = vocabService.get;

export function JoinMatchCard() {
  const user = useAuth();
  const { code, setCode, error, pending, onSubmit, watch } = useJoinMatch();

  return (
    <Card title={vocab('join_title')} description={vocab('join_description')}>
      <form className="flex flex-col gap-4" onSubmit={onSubmit} noValidate>
        <TextField
          label={vocab('join_code_label')}
          placeholder={vocab('join_code_placeholder')}
          value={code}
          onChange={(event) => setCode(event.target.value)}
          error={error}
          hint={user ? vocab('join_hint_account') : vocab('join_hint_guest')}
          maxLength={constants.room.codeLength}
          autoComplete="off"
          autoCapitalize="characters"
          spellCheck={false}
          className="[&_input]:font-mono [&_input]:text-lg [&_input]:uppercase [&_input]:tracking-[0.3em]"
        />
        <div className="flex flex-wrap gap-2">
          <Button
            type="submit"
            size="lg"
            loading={pending === 'join'}
            className="flex-1 sm:flex-none"
          >
            {vocab('join_submit')}
          </Button>
          <Button
            variant="secondary"
            size="lg"
            disabled={pending !== null}
            onClick={watch}
            className="flex-1 sm:flex-none"
          >
            {vocab('join_watch')}
          </Button>
        </div>
      </form>
    </Card>
  );
}
