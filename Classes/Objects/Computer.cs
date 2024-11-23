﻿using MySql.Data.MySqlClient;

namespace EduchemLPR.Classes.Objects;

public class Computer {
    private Computer(string id, uint? reservedBy, string? reservedByName) {
        ID = id;
        ReservedBy = reservedBy;
        ReservedByName = reservedByName;
    }

    public string ID { get; private set; }
    public uint? ReservedBy { get; private set; }
    public string? ReservedByName { get; private set; }

    public static async Task<List<Computer>> GetAllAsync() {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return [];

        var computers = new List<Computer>();
        await using var cmd = new MySqlCommand(@"
            SELECT c.*, 
                   u.display_name AS reserved_by_name
            FROM computers c
            LEFT JOIN users u ON c.reserved_by = u.id
        ", conn);
        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if(reader == null) return computers;

        while (await reader.ReadAsync()) {
            computers.Add(
                new Computer(
                    reader.GetString("id"),
                    reader.GetObjectOrNull("reserved_by") as uint?,
                    reader.GetObjectOrNull("reserved_by_name") as string
                )
            );
        }

        return computers;
    }

    public static List<Computer> GetAll() => GetAllAsync().Result;
}