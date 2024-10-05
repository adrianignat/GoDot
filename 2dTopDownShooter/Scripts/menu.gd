class_name MainMenu
extends Control

@onready var start_game = $MarginContainer/VBoxContainer/Start_Game as Button
@onready var exit_game = $MarginContainer/VBoxContainer/Exit_Game as Button
@onready var game_options = $MarginContainer/VBoxContainer/Game_Options as Button
@onready var start_level = preload("res://main.tscn") as PackedScene


# Called when the node enters the scene tree for the first time.
func _ready():
	start_game.button_down.connect(on_start_pressed)
	exit_game.button_down.connect(on_exit_pressed)
	

func on_start_pressed() -> void:
	get_tree().change_scene_to_packed(start_level)
	
func on_exit_pressed() -> void:
	get_tree().quit()

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass
