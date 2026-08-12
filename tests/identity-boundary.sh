#!/usr/bin/env bash
set -euo pipefail

base_url="${SHOPMICRO_BASE_URL:-http://localhost}"
admin_url="${SHOPMICRO_ADMIN_URL:-http://localhost:81}"
admin_email="${SHOPMICRO_TEST_ADMIN_EMAIL:?Defina SHOPMICRO_TEST_ADMIN_EMAIL}"
admin_password="${SHOPMICRO_TEST_ADMIN_PASSWORD:?Defina SHOPMICRO_TEST_ADMIN_PASSWORD}"
customer_email="identity-test-$(date +%s)@example.com"
customer_password="IdentityTest-123!"

json_value() {
  local key="$1"
  sed -n "s/.*\"${key}\":\"\([^\"]*\)\".*/\1/p"
}

expect_status() {
  local expected="$1"
  local actual="$2"
  local description="$3"
  if [[ "$actual" != "$expected" ]]; then
    printf 'FALHOU: %s (esperado %s, recebido %s)\n' "$description" "$expected" "$actual" >&2
    exit 1
  fi
  printf 'OK: %s\n' "$description"
}

register_status="$(curl -sS -o /tmp/shopmicro-register.json -w '%{http_code}' \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"${customer_email}\",\"password\":\"${customer_password}\",\"fullName\":\"Cliente de teste\"}" \
  "${base_url}/api/marketplace/accounts")"
expect_status 201 "$register_status" 'cadastro de cliente no marketplace'

customer_login="$(curl -sS -H 'Content-Type: application/json' \
  -d "{\"email\":\"${customer_email}\",\"password\":\"${customer_password}\"}" \
  "${base_url}/api/marketplace/auth/login")"
customer_token="$(printf '%s' "$customer_login" | json_value token)"
[[ -n "$customer_token" ]] || { echo 'FALHOU: token do cliente não retornado' >&2; exit 1; }

admin_login="$(curl -sS -H 'Content-Type: application/json' \
  -d "{\"email\":\"${admin_email}\",\"password\":\"${admin_password}\"}" \
  "${admin_url}/api/administration/auth/login")"
admin_token="$(printf '%s' "$admin_login" | json_value token)"
[[ -n "$admin_token" ]] || { echo 'FALHOU: token administrativo não retornado' >&2; exit 1; }

customer_admin_status="$(curl -sS -o /dev/null -w '%{http_code}' \
  -H "Authorization: Bearer ${customer_token}" \
  "${admin_url}/api/administration/accounts")"
expect_status 403 "$customer_admin_status" 'token do cliente bloqueado na administração'

admin_access_status="$(curl -sS -o /dev/null -w '%{http_code}' \
  -H "Authorization: Bearer ${admin_token}" \
  "${admin_url}/api/administration/accounts")"
expect_status 200 "$admin_access_status" 'token administrativo aceito na administração'

admin_marketplace_status="$(curl -sS -o /dev/null -w '%{http_code}' \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"${admin_email}\",\"password\":\"${admin_password}\"}" \
  "${base_url}/api/marketplace/auth/login")"
expect_status 401 "$admin_marketplace_status" 'conta administrativa bloqueada no login do marketplace'

delete_status="$(curl -sS -o /dev/null -w '%{http_code}' -X DELETE \
  -H "Authorization: Bearer ${admin_token}" \
  "${admin_url}/api/administration/marketplace-accounts/${customer_email}")"
expect_status 200 "$delete_status" 'limpeza da conta de teste'

echo 'Fronteira de identidade validada com sucesso.'
