const std = @import("std");

const ASCII_CHARS = " .:-=+*#%@";
const WIDTH = 80;
const HEIGHT = 40;

fn downloadVideo(url: []const u8) !void {
    std.debug.print("Downloading video...\n", .{});
    var child = std.ChildProcess.init(&[_][]const u8{ "yt-dlp", "-f", "worst", "-o", "video.mp4", url }, std.heap.page_allocator);
    _ = try child.spawnAndWait();
}

fn rgbToAscii(r: u8, g: u8, b: u8) u8 {
    const brightness = (@as(u16, r) + @as(u16, g) + @as(u16, b)) / 3;
    const index = brightness * (ASCII_CHARS.len - 1) / 255;
    return ASCII_CHARS[index];
}

fn extractAndDisplayFrame(allocator: std.mem.Allocator, time: f64) !void {
    const cmd = try std.fmt.allocPrint(allocator, "ffmpeg -ss {d:.2} -i video.mp4 -vframes 1 -vf scale={d}:{d} -f rawvideo -pix_fmt rgb24 - 2>/dev/null", .{ time, WIDTH, HEIGHT });
    defer allocator.free(cmd);
    
    var child = std.ChildProcess.init(&[_][]const u8{ "sh", "-c", cmd }, allocator);
    child.stdout_behavior = .Pipe;
    
    try child.spawn();
    const stdout = child.stdout.?.reader();
    const pixels = try stdout.readAllAlloc(allocator, WIDTH * HEIGHT * 3);
    defer allocator.free(pixels);
    
    _ = try child.wait();
    
    if (pixels.len > 0) {
        std.debug.print("\x1B[2J\x1B[H", .{});
        
        var y: usize = 0;
        while (y < HEIGHT) : (y += 1) {
            var x: usize = 0;
            while (x < WIDTH) : (x += 1) {
                const idx = (y * WIDTH + x) * 3;
                if (idx + 2 < pixels.len) {
                    const r = pixels[idx];
                    const g = pixels[idx + 1];
                    const b = pixels[idx + 2];
                    std.debug.print("{c}", .{rgbToAscii(r, g, b)});
                }
            }
            std.debug.print("\n", .{});
        }
    }
}

pub fn main() !void {
    var gpa = std.heap.GeneralPurposeAllocator(.{}){};
    defer _ = gpa.deinit();
    const allocator = gpa.allocator();
    
    const url = "https://youtu.be/FtutLA63Cp8";
    try downloadVideo(url);
    
    const fps = 10.0;
    const duration = 30.0;
    var time: f64 = 0.0;
    
    while (time < duration) {
        try extractAndDisplayFrame(allocator, time);
        std.time.sleep(@intFromFloat(1.0e9 / fps));
        time += 1.0 / fps;
    }
}
