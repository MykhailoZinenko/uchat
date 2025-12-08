# UChat API Documentation

## Подключение

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5000/chat")
    .withAutomaticReconnect()
    .build();

await connection.start();
```

## Endpoints

### 1. Register
**Регистрация нового пользователя**

```javascript
const result = await connection.invoke("Register", username, password, deviceInfo, ipAddress);
```

**Параметры:**
- `username` (string, required) - имя пользователя
- `password` (string, required) - пароль
- `deviceInfo` (string, optional) - информация об устройстве (например: "Chrome/Windows")
- `ipAddress` (string, optional) - IP адрес (автоматически определяется если null)

**Возвращает:**
```typescript
{
  success: boolean,
  message: string,
  data: {
    sessionToken: string  // Криптографически стойкий токен сессии (не меняется)
  }
}
```

---

### 2. Login
**Вход существующего пользователя**

```javascript
const result = await connection.invoke("Login", username, password, deviceInfo, ipAddress);
```

**Параметры:**
- `username` (string, required) - имя пользователя
- `password` (string, required) - пароль
- `deviceInfo` (string, optional) - информация об устройстве
- `ipAddress` (string, optional) - IP адрес

**Возвращает:**
```typescript
{
  success: boolean,
  message: string,
  data: {
    sessionToken: string  // Новая сессия = новый токен
  }
}
```

---

### 3. LoginWithRefreshToken
**Silent login - продление активной сессии**

```javascript
const result = await connection.invoke("LoginWithRefreshToken", sessionToken, deviceInfo, ipAddress);
```

**Параметры:**
- `sessionToken` (string, required) - токен сессии
- `deviceInfo` (string, optional) - обновить информацию об устройстве
- `ipAddress` (string, optional) - обновить IP адрес

**Возвращает:**
```typescript
{
  success: boolean,
  message: string,
  data: {
    sessionToken: string   // Тот же токен (НЕ меняется!)
  }
}
```

**Важно:** 
- Токен **не меняется** при использовании этого метода
- Автоматически продлевает срок действия сессии (ExpiresAt)
- Обновляет LastActivityAt

---

### 4. GetActiveSessions
**Получить список активных сессий пользователя**

```javascript
const result = await connection.invoke("GetActiveSessions", sessionToken);
```

**Параметры:**
- `sessionToken` (string, required) - токен сессии

**Возвращает:**
```typescript
{
  success: boolean,
  message: string,
  data: [
    {
      token: string,           // токен сессии
      deviceInfo: string,      // информация об устройстве
      createdAt: DateTime,     // дата создания
      lastActivityAt: DateTime,// последняя активность
      expiresAt: DateTime      // дата истечения
    }
  ]
}
```

---

### 5. RevokeSession
**Отозвать конкретную сессию**

```javascript
const result = await connection.invoke("RevokeSession", sessionToken, sessionId);
```

**Параметры:**
- `sessionToken` (string, required) - токен сессии
- `sessionId` (int, required) - ID сессии для отзыва

**Возвращает:**
```typescript
{
  success: boolean,
  message: string,
  data: boolean  // true если успешно
}
```

---

### 6. Logout
**Выход - отзыв текущей сессии**

```javascript
const result = await connection.invoke("Logout", sessionToken);
```

**Параметры:**
- `sessionToken` (string, required) - токен сессии

**Возвращает:**
```typescript
{
  success: boolean,
  message: string,
  data: boolean  // true если успешно
}
```

**Важно:** После logout токен сессии становится недействительным!

---

### 7. SendMessage
**Отправить сообщение в чат**

```javascript
await connection.invoke("SendMessage", sessionToken, message);
```

**Параметры:**
- `sessionToken` (string, required) - токен сессии
- `message` (string, required) - текст сообщения

**Возвращает:** void (ничего)

**События:**
- При успехе отправляет всем в группе "LoggedIn" событие `ReceiveMessage`
- При ошибке отправляет отправителю событие `Error`

---

## События SignalR

### ReceiveMessage
**Получение нового сообщения**

```javascript
connection.on("ReceiveMessage", (message) => {
  console.log(message);
  // {
  //   connectionId: string,
  //   username: string,
  //   content: string,
  //   sentAt: DateTime
  // }
});
```

### Error
**Получение ошибки**

```javascript
connection.on("Error", (errorMessage) => {
  console.error(errorMessage);
});
```

---

## Типичные сценарии

### Первичная авторизация
```javascript
// 1. Регистрация или логин
const authResult = await connection.invoke("Register", "user123", "pass123", "Chrome/Windows");
// или
const authResult = await connection.invoke("Login", "user123", "pass123", "Chrome/Windows");

// 2. Сохранить токен
localStorage.setItem('sessionToken', authResult.data.sessionToken);
```

### Автоматический вход (Silent Login)
```javascript
const sessionToken = localStorage.getItem('sessionToken');
const result = await connection.invoke("LoginWithRefreshToken", sessionToken);

if (result.success) {
  // Токен остался тот же, но сессия продлена
  console.log('Session extended');
} else {
  // Перенаправить на страницу логина
}
```

### Отправка сообщения
```javascript
const sessionToken = localStorage.getItem('sessionToken');
await connection.invoke("SendMessage", sessionToken, "Hello, World!");
```

### Выход
```javascript
const sessionToken = localStorage.getItem('sessionToken');
await connection.invoke("Logout", sessionToken);

localStorage.removeItem('sessionToken');
```

---

## Коды ошибок

Все методы возвращают объект с полем `success`:
- `true` - операция успешна
- `false` - произошла ошибка (см. поле `message`)

**Типичные ошибки:**
- `"Invalid or expired refresh token"` - токен сессии недействителен или истёк
- `"Session has expired"` - сессия истекла
- `"Invalid session token"` - токен не найден
- `"User not found or invalid password"` - неверные учетные данные
- `"Username already exists"` - пользователь уже существует
- `"Session expired or not found"` - сессия истекла или удалена
- `"Internal server error"` - внутренняя ошибка сервера

---

## Время жизни токена

- **Session Token**: 30 дней (2592000000 ms)

**Автоматическое продление:**
- При каждом использовании токена (любой метод API) срок действия автоматически продлевается на 30 дней
- Таким образом, пока пользователь активен - сессия не истекает
- Если пользователь не использует приложение 30 дней - сессия истекает

**Рекомендации:**
- Используйте `LoginWithRefreshToken` при запуске приложения для проверки токена
- Храните session token безопасно (HttpOnly cookie или secure storage)
- Не нужно обновлять токен вручную - он продлевается автоматически
