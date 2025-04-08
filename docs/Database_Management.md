# Database Management

ADEPT uses SQLite for data storage, with a robust database management system that includes backup, recovery, and maintenance features.

## Database Structure

The application uses a SQLite database with the following tables:

- **Classes**: Stores information about classes
- **Students**: Stores information about students
- **LessonPlans**: Stores lesson plans
- **Conversations**: Stores conversation history with the LLM
- **SystemPrompts**: Stores system prompts for the LLM
- **DatabaseVersions**: Tracks database schema versions for migrations

## Database Backup and Recovery

ADEPT includes a comprehensive backup and recovery system to protect your data.

### Automatic Backups

The application automatically creates backups in the following situations:

1. **Before Migrations**: When the database schema is updated, a backup is created automatically
2. **Periodic Backups**: The application can be configured to create backups at regular intervals
3. **Initial Backup**: A backup is created when the application is first run

### Manual Backups

You can create manual backups from the Configuration screen:

1. Go to the **Configuration** tab
2. Select the **Database** tab
3. Enter an optional name for the backup (or leave blank for a timestamp-based name)
4. Click **Create Backup**

### Restoring from Backup

To restore the database from a backup:

1. Go to the **Configuration** tab
2. Select the **Database** tab
3. Select a backup from the list
4. Click **Restore Selected Backup**

The application will create a backup of the current database before restoring, so you can always revert if needed.

## Database Maintenance

ADEPT includes tools to maintain database health and performance.

### Integrity Check

To verify the integrity of the database:

1. Go to the **Configuration** tab
2. Select the **Database** tab
3. Click **Verify Integrity**

This will check for database corruption and foreign key violations.

### Database Optimization

To optimize the database:

1. Go to the **Configuration** tab
2. Select the **Database** tab
3. Click **Perform Maintenance**

This will:
- Rebuild the database file (VACUUM)
- Update database statistics (ANALYZE)
- Optimize the database (PRAGMA optimize)

## Configuration

Database settings can be configured in the `appsettings.json` file:

```json
"Database": {
  "ConnectionString": "Data Source=data/adept.db",
  "BackupDirectory": "data/backups",
  "MaxBackupCount": 10,
  "AutoBackupIntervalHours": 24
}
```

- **ConnectionString**: The SQLite connection string
- **BackupDirectory**: The directory where backups are stored
- **MaxBackupCount**: The maximum number of backups to keep (oldest are deleted)
- **AutoBackupIntervalHours**: How often to create automatic backups (in hours)

## Data Validation

ADEPT includes comprehensive data validation to ensure data integrity:

1. **Application-Level Validation**: All entities are validated before being saved to the database
2. **Database-Level Validation**: SQLite triggers enforce data integrity rules
3. **Foreign Key Constraints**: Ensure referential integrity between tables

## Technical Details

The database implementation includes:

- **Repository Pattern**: Each entity type has a dedicated repository
- **Unit of Work**: Transactions ensure data consistency
- **Migrations**: Automatic schema updates
- **Connection Pooling**: Efficient database connections
- **Async Operations**: Non-blocking database access
